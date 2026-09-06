using System;
using Graphaclysm.Core.Equations;
using UnityEngine;

namespace Graphaclysm.Runtime.Presentation
{
    /// <summary>
    /// One material per view lifetime. Reads at most MaximumSegments directly from
    /// Core's reusable contour buffer; creates no per-frame geometry or collections.
    /// Draw must run during a top-level IMGUI Repaint, before the enemy overlays.
    /// </summary>
    internal sealed class PolynomiographLineRenderer : IDisposable
    {
        private Material material;

        public PolynomiographLineRenderer()
        {
            Shader shader = Resources.Load<Shader>("PolynomiographLines");
            if (shader == null || !shader.isSupported)
            {
                throw new InvalidOperationException("PolynomiographLines shader is missing or unsupported.");
            }

            material = new Material(shader)
            {
                name = "GRAPHACLYSM contour lines",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        public void Draw(EquationState equation, Rect rect, int visibleSegments, Color color, float thickness, float progress = 1f)
        {
            if (Event.current.type != EventType.Repaint || visibleSegments <= 0)
            {
                return;
            }

            if (!material.SetPass(0)) return;

            float scaleX = rect.width / (float)(PolynomiographContourSet.DomainMaximum
                - PolynomiographContourSet.DomainMinimum);
            float scaleY = rect.height / (float)(PolynomiographContourSet.RangeMaximum
                - PolynomiographContourSet.RangeMinimum);
            float offsetX = rect.x - (float)PolynomiographContourSet.DomainMinimum * scaleX;
            float offsetY = rect.yMax + (float)PolynomiographContourSet.RangeMinimum * scaleY;
            float halfThickness = thickness * 0.5f;

            GL.PushMatrix();
            try
            {
                GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
                GL.MultMatrix(GUI.matrix);
                GL.Begin(GL.QUADS);
                try
                {
                    GL.Color(color * GUI.color);
                    for (int i = 0; i < visibleSegments; i++)
                    {
                        equation.GetFieldLineSegment(i, out double x0, out double y0,
                            out double x1, out double y1);
                        if (!GraphSegmentClipper.ClipReveal(progress, ref x0, ref y0, ref x1, ref y1)) continue;
                        float startX = offsetX + (float)x0 * scaleX;
                        float startY = offsetY - (float)y0 * scaleY;
                        float endX = offsetX + (float)x1 * scaleX;
                        float endY = offsetY - (float)y1 * scaleY;
                        float dx = endX - startX;
                        float dy = endY - startY;
                        float lengthSquared = dx * dx + dy * dy;
                        if (lengthSquared < 0.0001f) continue;

                        float normalScale = halfThickness / Mathf.Sqrt(lengthSquared);
                        float nx = -dy * normalScale;
                        float ny = dx * normalScale;
                        GL.Vertex3(startX + nx, startY + ny, 0f);
                        GL.Vertex3(endX + nx, endY + ny, 0f);
                        GL.Vertex3(endX - nx, endY - ny, 0f);
                        GL.Vertex3(startX - nx, startY - ny, 0f);
                    }
                }
                finally
                {
                    GL.End();
                }
            }
            finally
            {
                GL.PopMatrix();
            }
        }

        public void Dispose()
        {
            if (material == null) return;
            UnityEngine.Object.Destroy(material);
            material = null;
        }
    }
}

using System;
using Graphaclysm.Core.Equations;
using UnityEngine;

namespace Graphaclysm.Runtime.Presentation
{
    /// <summary>One reusable material. Shared Core segments, three feathered ink layers, no frame buffers.</summary>
    internal sealed class AstralSpellRenderer : IDisposable
    {
        private readonly Material ink;
        public AstralSpellRenderer()
        {
            Shader shader = Resources.Load<Shader>("AstralInk");
            if (shader == null) throw new InvalidOperationException("AstralInk shader is missing.");
            ink = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public void Draw(EquationState equation, Rect rect, float elapsed, bool casting)
        {
            if (Event.current.type != EventType.Repaint || !equation.HasBase || !ink.SetPass(0)) return;
            float reveal = casting ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - 0.18f) / 0.78f)) : 1;
            float release = casting ? Mathf.Clamp01((elapsed - 1.05f) / 0.7f) : 0;
            double originX = equation.IsFragmentMode ? equation.Fragments.OriginX : 5, originY = equation.IsFragmentMode ? equation.Fragments.OriginY : 0;
            double farX = Math.Max(originX,10-originX), farY = Math.Max(originY+4,4-originY);
            int count = equation.IsFieldFunction ? equation.FieldLineSegmentCount : equation.CurveSegmentCount;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0); GL.MultMatrix(GUI.matrix);
            GL.Begin(GL.QUADS);
            for (int i = 0; i < count; i++)
            {
                double x0, y0, x1, y1;
                if (equation.IsFieldFunction) equation.GetFieldLineSegment(i, out x0, out y0, out x1, out y1);
                else
                {
                    equation.Sample(i / (double)count, out x0, out y0);
                    equation.Sample((i + 1) / (double)count, out x1, out y1);
                    if (!GraphSegmentClipper.ClipToField(ref x0, ref y0, ref x1, ref y1)) continue;
                }
                Vector2 a = ToScreen(rect, x0, y0), b = ToScreen(rect, x1, y1);
                Emit(a, b, casting ? 2 : 3, new Color(0.65f, 0.59f, 0.85f, casting ? 0.2f : 0.68f));
                if (!casting || !GraphSegmentClipper.ClipReveal(reveal, ref x0, ref y0, ref x1, ref y1, originX, originY)) continue;
                a = ToScreen(rect, x0, y0); b = ToScreen(rect, x1, y1);
                float shimmer = 0.84f + Mathf.Sin(i * 0.19f - elapsed * 4) * 0.12f;
                Emit(a, b, 22 - release * 10, new Color(0.62f, 0.38f, 1f, 0.23f * (1 - release)));
                Emit(a, b, 8, new Color(0.77f, 0.61f, 1, 0.68f * shimmer * (1 - release * 0.6f)));
                Emit(a, b, 2.8f, new Color(1, 0.97f, 0.88f, 1 - release * 0.4f));
                double distance = Math.Sqrt((x1 - originX) * (x1 - originX) + (y1-originY) * (y1-originY));
                if (reveal < 0.99f && Math.Abs(distance - Math.Sqrt(farX*farX+farY*farY) * reveal) < 0.025)
                {
                    Emit(b - Vector2.right * 13, b + Vector2.right * 13, 5, new Color(1, 0.94f, 1, 0.9f));
                    Emit(b - Vector2.up * 11, b + Vector2.up * 11, 3, new Color(1, 0.96f, 0.87f, 0.9f));
                }
            }
            GL.End(); GL.PopMatrix();
        }

        private static Vector2 ToScreen(Rect r, double x, double y)
            => new Vector2(r.x + (float)x * r.width / 10, r.yMax - ((float)y + 4) * r.height / 8);

        private static void Emit(Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            GL.Color(color * GUI.color);
            GL.TexCoord2(0, 0); GL.Vertex3(a.x - n.x, a.y - n.y, 0);
            GL.TexCoord2(1, 0); GL.Vertex3(b.x - n.x, b.y - n.y, 0);
            GL.TexCoord2(1, 1); GL.Vertex3(b.x + n.x, b.y + n.y, 0);
            GL.TexCoord2(0, 1); GL.Vertex3(a.x + n.x, a.y + n.y, 0);
        }

        public void Dispose() { if (ink != null) UnityEngine.Object.Destroy(ink); }
    }
}

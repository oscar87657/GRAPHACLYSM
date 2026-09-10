using Graphaclysm.Core.Cards;
using UnityEngine;

namespace Graphaclysm.Runtime.Presentation
{
    /// <summary>Shared visual vocabulary. Styles and fonts are owned by one view lifetime.</summary>
    internal sealed class AstralUi
    {
        public static readonly Color Ink = new Color(0.105f, 0.105f, 0.16f);
        public static readonly Color Paper = new Color(0.94f, 0.93f, 0.92f);
        public static readonly Color Violet = new Color(0.53f, 0.43f, 0.7f);
        public static readonly Color Gold = new Color(0.71f, 0.63f, 0.47f);
        public static readonly Color Muted = new Color(0.43f, 0.41f, 0.49f);
        public static readonly Color Threat = new Color(0.8f, 0.43f, 0.53f);
        public readonly GUIStyle Logo, Display, PageTitle, Heading, Body, Small, Number, Light, SmallLight, Formula;

        private readonly System.Action onClick;

        public AstralUi(System.Action onClick = null)
        {
            this.onClick = onClick;
            Font body = Resources.Load<Font>("Fonts/Pretendard-Regular");
            Font orbit = Resources.Load<Font>("Fonts/Orbit-Regular");
            Font serif = Resources.Load<Font>("Fonts/CormorantGaramond-Light");
            if (body == null || orbit == null || serif == null) throw new System.InvalidOperationException("Bundled presentation fonts are missing.");
            Logo = Style(serif, 100, Ink); Display = Style(serif, 58, Ink);
            PageTitle = Style(orbit, 40, Ink);
            Heading = Style(orbit, 26, Ink); Body = Style(body, 21, Ink);
            Small = Style(body, 17, Muted); Number = Style(orbit, 28, Ink);
            Light = Style(body, 21, Paper); SmallLight = Style(body, 17, new Color(0.74f, 0.71f, 0.82f));
            Formula = Style(orbit, 21, Paper);
        }

        private static GUIStyle Style(Font font, int size, Color color)
        {
            var style = new GUIStyle { font = font, fontSize = size, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            style.normal.textColor = color; return style;
        }

        public static void Label(Rect rect, string text, GUIStyle style, bool center = false)
        {
            var previous = style.alignment;
            if (center) style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(rect, text, style); style.alignment = previous;
        }

        public bool Button(Rect r, string text, bool primary = false, bool enabled = true)
        {
            bool hover = GUI.enabled && enabled && r.Contains(Event.current.mousePosition);
            Fill(r, primary ? (hover ? Violet : Ink) : new Color(1, 1, 1, hover ? 0.78f : 0.28f));
            Border(r, enabled ? (primary ? Gold : new Color(0.57f, 0.52f, 0.63f, 0.5f)) : new Color(0.6f, 0.58f, 0.62f, 0.3f));
            float inset = r.width < 100 ? 3 : 16;
            Label(new Rect(r.x + inset, r.y, r.width - inset * 2, r.height), text, primary ? Light : Body, true);
            bool old = GUI.enabled; GUI.enabled = old && enabled;
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = old; if (clicked) onClick?.Invoke(); return clicked;
        }

        public static void Fill(Rect r, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }

        public static void Line(Vector2 a, Vector2 b, Color color, float width = 1)
        {
            Vector2 delta = b - a; if (delta.sqrMagnitude < 0.0001f) return;
            Matrix4x4 matrix = GUI.matrix; Color saved = GUI.color;
            GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(a.x, a.y, 0),
                Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg), Vector3.one);
            GUI.color = color; GUI.DrawTexture(new Rect(0, -width / 2, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.color = saved; GUI.matrix = matrix;
        }

        public static void Border(Rect r, Color color, float cut = 12)
        {
            Line(new Vector2(r.x + cut, r.y), new Vector2(r.xMax, r.y), color);
            Line(new Vector2(r.xMax, r.y), new Vector2(r.xMax, r.yMax - cut), color);
            Line(new Vector2(r.xMax, r.yMax - cut), new Vector2(r.xMax - cut, r.yMax), color);
            Line(new Vector2(r.xMax - cut, r.yMax), new Vector2(r.x, r.yMax), color);
            Line(new Vector2(r.x, r.yMax), new Vector2(r.x, r.y + cut), color);
            Line(new Vector2(r.x, r.y + cut), new Vector2(r.x + cut, r.y), color);
        }

        public static void Diamond(Vector2 center, float radius, Color color, float width = 1)
        {
            Line(center + Vector2.up * radius, center + Vector2.right * radius, color, width);
            Line(center + Vector2.right * radius, center + Vector2.down * radius, color, width);
            Line(center + Vector2.down * radius, center + Vector2.left * radius, color, width);
            Line(center + Vector2.left * radius, center + Vector2.up * radius, color, width);
        }

        public static void Ring(Vector2 center, float radius, Color color, float width = 1, float fraction = 1, float rotation = 0)
        {
            const int samples = 80;
            Vector2 previous = center + new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * radius;
            for (int i = 1; i <= samples; i++)
            {
                float angle = rotation + i * Mathf.PI * 2 * fraction / samples;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Line(previous, next, color, width); previous = next;
            }
        }

        public static Color Rarity(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Uncommon: return new Color(0.36f, 0.64f, 0.62f);
                case CardRarity.Rare: return new Color(0.66f, 0.51f, 0.8f);
                case CardRarity.Legendary: return Gold;
                default: return new Color(0.55f, 0.57f, 0.66f);
            }
        }
    }
}

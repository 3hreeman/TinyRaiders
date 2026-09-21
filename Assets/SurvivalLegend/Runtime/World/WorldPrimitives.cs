using UnityEngine;

namespace SurvivalLegend.World
{
    public static class WorldPrimitives
    {
        public static Color Parse(string html, Color fallback)
        {
            return !string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out var value) ? value : fallback;
        }

        public static void Line(LineRenderer line, Vector3 start, Vector3 end, float width, Color color)
        {
            if (line == null) return;
            line.enabled = true;
            line.loop = false;
            line.positionCount = 2;
            line.SetPosition(0, start); line.SetPosition(1, end);
            line.startWidth = width; line.endWidth = width;
            line.startColor = color; line.endColor = color;
        }

        public static void Ring(LineRenderer line, Vector2 center, float radius, Color color, int segments = 48)
        {
            if (line == null) return;
            line.enabled = true; line.loop = true; line.positionCount = segments;
            line.startColor = color; line.endColor = color;
            line.startWidth = .015f; line.endWidth = .015f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                line.SetPosition(i, WorldProjection.WorldToScene(center + offset));
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace SurvivalLegend.UI
{
    /// <summary>Small UI vertex glints; works in Screen Space Overlay without a particle camera.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AugmentSparkleGraphic : MaskableGraphic
    {
        private string tier = "silver";
        private float animationTime, strength, revealProgress = 1f;

        public void SetAnimation(string tierName, float unscaledTime, float revealStrength, float arrivalProgress = 1f)
        {
            tier = tierName ?? "silver";
            animationTime = unscaledTime;
            strength = Mathf.Clamp01(revealStrength);
            revealProgress = Mathf.Clamp01(arrivalProgress);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (strength <= .01f) return;
            Rect rect = rectTransform.rect;
            const int count = 15;
            for (int i = 0; i < count; i++)
            {
                float seed = Hash(i * 13 + 7);
                float phase = Mathf.Repeat(animationTime * (.35f + Hash(i * 17 + 3) * .35f) + seed, 1f);
                float life = Mathf.Sin(phase * Mathf.PI);
                float alpha = life * life * strength * (tier == "silver" ? .43f : .72f);
                if (alpha < .01f) continue;
                float side = i % 4;
                float travel = .12f + Hash(i * 29 + 9) * .76f;
                float x = side < 2 ? Mathf.Lerp(rect.xMin + 7, rect.xMax - 7, travel)
                    : side == 2 ? rect.xMin + 11 : rect.xMax - 11;
                float y = side < 2 ? side == 0 ? rect.yMax - 10 : rect.yMin + 10
                    : Mathf.Lerp(rect.yMin + 11, rect.yMax - 11, travel);
                x += Mathf.Sin(animationTime * 1.8f + i * 2.7f) * 2f;
                y += Mathf.Cos(animationTime * 1.5f + i * 3.1f) * 3f;
                float size = 1.5f + Hash(i * 11 + 5) * 2.4f;
                Color color = ColorFor(i);
                color.a *= alpha;
                int vertex = helper.currentVertCount;
                helper.AddVert(new Vector3(x, y + size), color, Vector2.zero);
                helper.AddVert(new Vector3(x + size, y), color, Vector2.zero);
                helper.AddVert(new Vector3(x, y - size), color, Vector2.zero);
                helper.AddVert(new Vector3(x - size, y), color, Vector2.zero);
                helper.AddTriangle(vertex, vertex + 1, vertex + 2);
                helper.AddTriangle(vertex, vertex + 2, vertex + 3);
            }
            // Short radial burst from the icon medallion while the cards unfold.
            // The mesh stays in the overlay Canvas, avoiding a second particle camera.
            float burst = Mathf.Pow(Mathf.Sin(Mathf.PI * revealProgress), 1.35f);
            if (burst < .015f || revealProgress >= 1f) return;
            int rays = tier == "prism" || tier == "special" ? 32 : tier == "gold" ? 20 : 12;
            Vector2 origin = new Vector2(0, rect.yMax - 93f);
            for (int i = 0; i < rays; i++)
            {
                float phase = (i + Hash(i * 31 + 4) * .42f) / rays;
                float angle = phase * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 side = new Vector2(-direction.y, direction.x);
                float radius = 47f + revealProgress * (65f + Hash(i * 19 + 2) * 34f);
                float length = 7f + Hash(i * 23 + 1) * 17f;
                float width = 1.1f + Hash(i * 7 + 8) * 1.4f;
                Vector2 center = origin + direction * radius;
                Color color = ColorFor(i);
                color.a *= burst * (tier == "silver" ? .58f : .82f);
                int vertex = helper.currentVertCount;
                helper.AddVert(center - direction * length - side * width, color, Vector2.zero);
                helper.AddVert(center + direction * length, color, Vector2.zero);
                helper.AddVert(center - direction * length + side * width, color, Vector2.zero);
                helper.AddTriangle(vertex, vertex + 1, vertex + 2);
            }
        }

        private Color ColorFor(int index)
        {
            if (tier == "gold") return index % 3 == 0 ? new Color(1f, .96f, .76f) : new Color(1f, .71f, .28f);
            if (tier == "prism" || tier == "special")
                return index % 3 == 0 ? new Color(.51f, 1f, 1f)
                    : index % 3 == 1 ? new Color(1f, .51f, .89f) : new Color(.78f, .64f, 1f);
            return index % 3 == 0 ? Color.white : new Color(.68f, .83f, 1f);
        }
        private static float Hash(int n) => Mathf.Repeat(Mathf.Sin(n * 127.1f) * 43758.5453f, 1f);
    }
}

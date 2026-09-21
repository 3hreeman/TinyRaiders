using UnityEngine;

namespace SurvivalLegend.World
{
    public sealed class WorldEffectView : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;
        [SerializeField] private TextMesh textLabel;
        public long BoundId { get; private set; } = -1;

        public void Bind(EffectState effect)
        {
            BoundId = effect.Id;
            float age = 1f - effect.Life / Mathf.Max(.001f, effect.MaxLife);
            Color color = WorldPrimitives.Parse(effect.Color, Color.white);
            color.a *= 1f - age;
            Vector3 origin = WorldProjection.WorldToScene(effect.Position, effect.Elevation);
            transform.position = origin;
            if (textLabel != null)
            {
                bool text = effect.Kind == "text";
                textLabel.gameObject.SetActive(text);
                if (text)
                {
                    textLabel.text = effect.Text;
                    textLabel.color = color;
                    textLabel.characterSize = effect.Critical ? .036f : .026f;
                    textLabel.transform.localPosition = new Vector3(0, .7f + age * .24f, 0);
                }
            }
            if (line == null) return;
            if (effect.Kind == "text") { line.enabled = false; return; }
            if (effect.Kind == "lightning")
            {
                const int segments = 10;
                line.enabled = true; line.loop = false; line.positionCount = segments + 1;
                var end = WorldProjection.WorldToScene(effect.End);
                Vector3 delta = end - origin, normal = new Vector3(-delta.y, delta.x).normalized;
                int frame = Mathf.FloorToInt(age * 42f);
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float noise = Mathf.Sin((i + 1) * 127.1f + (float)(effect.Id % 100000) * .0317f + frame * 73.3f) * 43758.5453f;
                    float offset = i == segments || i == 0 ? 0 : (noise - Mathf.Floor(noise)) * .18f - .09f;
                    line.SetPosition(i, origin + delta * t + normal * offset);
                }
                line.startWidth = line.endWidth = .025f;
            }
            else if (effect.Kind == "slash")
            {
                var end = effect.End == Vector2.zero ? origin + new Vector3(effect.Radius / 100f, effect.Radius / 400f)
                    : WorldProjection.WorldToScene(effect.End);
                WorldPrimitives.Line(line, origin, end, Mathf.Lerp(.07f, .02f, age), color);
                return;
            }
            else
            {
                line.enabled = true; line.loop = true;
                line.positionCount = 40;
                float radius = Mathf.Max(10, effect.Radius * (.35f + age * .7f));
                for (int i = 0; i < 40; i++)
                {
                    float a = i * Mathf.PI * 2 / 40;
                    line.SetPosition(i, origin + new Vector3(Mathf.Cos(a) * radius / 100f,
                        Mathf.Sin(a) * radius * .4f / 100f, 0));
                }
                line.startWidth = line.endWidth = .02f;
            }
            line.startColor = color; line.endColor = color;
        }
        public void ResetView() { BoundId = -1; if (line != null) line.enabled = false; if (textLabel != null) textLabel.gameObject.SetActive(false); }
#if UNITY_EDITOR
        public void EditorSet(LineRenderer effectLine, TextMesh label) { line = effectLine; textLabel = label; }
#endif
    }
}

using UnityEngine;

namespace SurvivalLegend.World
{
    public sealed class ZoneView : MonoBehaviour
    {
        [SerializeField] private LineRenderer outline, core;
        [SerializeField] private bool particleFxEnabled;
        public int BoundId { get; private set; } = -1;
        public void Bind(ZoneState zone)
        {
            BoundId = zone.Id;
            transform.position = WorldProjection.WorldToScene(zone.Position);
            if(particleFxEnabled&&((zone.Visual=="rain"&&zone.TicksLeft<0)||(!zone.Hostile&&zone.Visual!="sword")))
            {if(outline!=null)outline.enabled=false;if(core!=null)core.enabled=false;return;}
            Color color = zone.Hostile ? new Color(1f, .42f, .38f, zone.Warning > 0 ? .72f : .5f) : new Color(.72f, .9f, .58f, .6f);
            switch (zone.Visual)
            {
                case "rain": color = new Color(.91f, 1f, .76f, .64f); break;
                case "spin": color = new Color(1f, .82f, .48f, .7f); break;
                case "sword": color = new Color(1f, .89f, .63f, .85f); break;
                case "blackhole": color = new Color(.68f, .44f, 1f, .72f); break;
            }
            if (zone.Shape == "circle")
            {
                WorldPrimitives.Ring(outline, zone.Position, zone.Radius, color);
                if (core != null)
                {
                    core.enabled = zone.CoreRadius > 0;
                    if (core.enabled) WorldPrimitives.Ring(core, zone.Position, zone.CoreRadius,
                        new Color(1f, .95f, .72f, color.a * .72f));
                }
            }
            else
            {
                Vector2 end = zone.Position + new Vector2(Mathf.Cos(zone.Angle), Mathf.Sin(zone.Angle)) * zone.Length;
                WorldPrimitives.Line(outline, WorldProjection.WorldToScene(zone.Position), WorldProjection.WorldToScene(end),
                    Mathf.Max(.03f, zone.Width * .00155f), color);
                if (core != null) core.enabled = false;
            }
        }
        public void ResetView() { BoundId = -1; if (outline != null) outline.enabled = false; if (core != null) core.enabled = false; }
#if UNITY_EDITOR
        public void EditorSet(LineRenderer ring, LineRenderer inner) { outline = ring; core = inner; }
        public void EditorEnableParticles(bool enabled){particleFxEnabled=enabled;}
#endif
    }
}

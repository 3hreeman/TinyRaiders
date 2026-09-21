using SurvivalLegend.Data;
using UnityEngine;

namespace SurvivalLegend.World
{
    public sealed class AimMarkerView : MonoBehaviour
    {
        [SerializeField] private LineRenderer primary, secondary, destination;
        public void Bind(SurvivorGame game)
        {
            if (destination != null)
            {
                var orderRun = game != null ? game.State : null;
                destination.enabled = orderRun != null && orderRun.Phase == RunPhase.Playing && orderRun.Destination.HasValue;
                if (destination.enabled)
                    WorldPrimitives.Ring(destination, orderRun.Destination.Value, 20,
                        orderRun.Order == "attack-move" ? new Color(1f, .72f, .42f, .8f) : new Color(.7f, .97f, .51f, .65f), 24);
            }
            if (game == null || game.State.Phase != RunPhase.Playing || game.State.AimSlot < 0)
            { ClearAim(); return; }
            RunState run = game.State;
            Vector2 origin = run.Player.Position, cursor = game.CursorWorld;
            Vector2 delta = cursor - origin;
            int slot = run.AimSlot;
            Color color = game.Content != null && game.Content.Characters.TryGetValue(run.Character, out var character)
                ? WorldPrimitives.Parse(character.Color, new Color(.74f, .93f, .62f))
                : new Color(.74f, .93f, .62f);
            color.a = .8f;
            if (slot == 6)
            {
                WorldPrimitives.Line(primary, WorldProjection.WorldToScene(origin), WorldProjection.WorldToScene(cursor), .015f, color);
                if (secondary != null) secondary.enabled = false;
                return;
            }
            if (slot >= 4)
            {
                int special = slot - 4;
                if (special >= run.Specials.Length || run.Specials[special].Id != "blackhole") { ClearAim(); return; }
                var data = game.Content.Specials["blackhole"];
                Vector2 center = origin + Vector2.ClampMagnitude(delta, data.Range);
                WorldPrimitives.Ring(primary, center, data.Radius, color);
                WorldPrimitives.Ring(secondary, origin, data.Range, new Color(color.r, color.g, color.b, .2f));
                return;
            }
            if (slot >= run.Skills.Length) { ClearAim(); return; }
            TargetingData target = game.Simulation.GetSkillTargeting(slot);
            if (target.Kind == TargetKind.Self)
            {
                WorldPrimitives.Ring(primary, origin, Mathf.Max(35, target.Radius), color);
                if (secondary != null) secondary.enabled = false;
            }
            else if (target.Kind == TargetKind.Point)
            {
                Vector2 center = origin + Vector2.ClampMagnitude(delta, target.Range);
                WorldPrimitives.Ring(primary, center, Mathf.Max(18, target.Radius), color);
                WorldPrimitives.Ring(secondary, origin, target.Range, new Color(color.r, color.g, color.b, .2f));
            }
            else
            {
                Vector2 direction = delta.sqrMagnitude > .001f ? delta.normalized : new Vector2(Mathf.Cos(run.Player.Facing), Mathf.Sin(run.Player.Facing));
                WorldPrimitives.Line(primary, WorldProjection.WorldToScene(origin),
                    WorldProjection.WorldToScene(origin + direction * target.Range), .015f, color);
                if (secondary != null) secondary.enabled = false;
            }
        }
        private void ClearAim() { if (primary != null) primary.enabled = false; if (secondary != null) secondary.enabled = false; }
        public void Clear() { ClearAim(); if (destination != null) destination.enabled = false; }
#if UNITY_EDITOR
        public void EditorSet(LineRenderer first, LineRenderer second, LineRenderer order)
        { primary = first; secondary = second; destination = order; }
#endif
    }
}

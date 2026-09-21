using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalLegend.World
{
    /// <summary>A saved, selectable actor hierarchy bound to simulation state at its foot.</summary>
    [RequireComponent(typeof(SortingGroup))]
    public sealed class ActorView : MonoBehaviour
    {
        [SerializeField] private DirectionalSpriteSet spriteSet;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private SpriteRenderer shield;
        [SerializeField] private SpriteRenderer healthBackground;
        [SerializeField] private SpriteRenderer healthFill;
        [SerializeField] private TextMesh nameLabel;
        [SerializeField] private LineRenderer windupLine;
        [SerializeField] private LineRenderer empowermentRing;
        [SerializeField] private SpriteRenderer[] overloadOrbs;
        [SerializeField] private float displayScale = .55f;
        [SerializeField] private Color accent = Color.white;
        private SortingGroup sortingGroup;

        public int BoundId { get; private set; } = -1;
        public Vector2 LogicalPosition { get; private set; }
        public string VisualId { get; private set; }

        private void Awake() => sortingGroup = GetComponent<SortingGroup>();

        public void SetDefinition(ActorVisualDefinition definition)
        {
            if (definition == null) return;
            VisualId = definition.id;
            if (definition.spriteSet != null) spriteSet = definition.spriteSet;
            if (definition.displayScale > 0) displayScale = definition.displayScale;
            accent = definition.accent;
        }

        public void BindPlayer(RunState run)
        {
            if (windupLine != null) windupLine.enabled = false;
            PlayerState player = run.Player;
            BoundId = 0;
            LogicalPosition = player.Position;
            Place(player.Position);
            bool spinning = run.Zones.Exists(z => z.FollowCaster && z.Visual == "spin" && z.Life > 0);
            bool attacking = player.AttackAnimation > 0 || spinning;
            float progress = player.AttackAnimation > 0 && player.MotionDuration > 0
                ? 1f - player.AttackAnimation / player.MotionDuration : 0f;
            float facing = player.AttackAnimation > 0 ? player.MotionAngle : player.Facing;
            if (spinning) { facing += run.Time * 18f; progress = Mathf.Repeat(run.Time * 3f, 1f); }
            if (body != null)
            {
                body.sprite = spriteSet != null ? spriteSet.Get(facing, player.Moving, attacking, progress, run.Time) : null;
                Color tint = run.Phase == RunPhase.Dying ? new Color(1, 1, 1, Mathf.Clamp01(1 - run.PhaseElapsed / 1.2f)) : Color.white;
                if (player.Motion == "blink") tint.a *= .55f + .45f * Mathf.Abs(Mathf.Sin(run.Time * 12f));
                body.color = tint;
                body.transform.localScale = Vector3.one * displayScale;
            }
            SetShadow(22, 10, new Color(accent.r, accent.g, accent.b, .28f));
            SetShield(player.Invulnerable > 0 || player.Shield > 0 || player.SkillShield > 0 || run.DodgeRemaining > 0,
                new Color(.84f, 1f, .71f, .8f), 30, 40);
            SetHealth(42, 43, player.Hp / Mathf.Max(1, player.MaxHp), player.Hp / Mathf.Max(1, player.MaxHp) > .3f
                ? new Color(.7f, .97f, .51f) : new Color(1f, .41f, .41f));
            SetLabel("YOU", -14, Color.white);
            bool empowered = run.Buffs.Exists(b => b.NextAttackCharges > 0);
            if (empowermentRing != null)
            {
                empowermentRing.enabled = empowered;
                if (empowered) WorldPrimitives.Ring(empowermentRing, player.Position, 42,
                    new Color(1f, .88f, .42f, .75f));
            }
            bool overloaded = run.Buffs.Exists(b => b.CooldownRate > 1 && b.Damage > 1);
            if (overloadOrbs != null)
                for (int i = 0; i < overloadOrbs.Length; i++)
                {
                    var orb = overloadOrbs[i]; if (orb == null) continue;
                    orb.enabled = overloaded;
                    if (!overloaded) continue;
                    float a = run.Time * 3f + i * Mathf.PI * 2f / 3f;
                    orb.transform.localPosition = new Vector3(Mathf.Cos(a) * .17f,
                        .25f - Mathf.Sin(a) * .07f, 0);
                }
        }

        public void BindEnemy(EnemyState enemy, float simulationTime, bool elite, string displayName)
        {
            BoundId = enemy.Id;
            LogicalPosition = enemy.Position;
            Place(enemy.Position);
            if (windupLine != null)
            {
                if (enemy.Windup > 0)
                    WorldPrimitives.Line(windupLine, WorldProjection.WorldToScene(enemy.Position),
                        WorldProjection.WorldToScene(enemy.Aim), .02f, new Color(1f, .62f, .45f, .75f));
                else windupLine.enabled = false;
            }
            if (empowermentRing != null) empowermentRing.enabled = false;
            if (overloadOrbs != null) foreach (var orb in overloadOrbs) if (orb != null) orb.enabled = false;
            if (body != null)
            {
                body.sprite = spriteSet != null ? spriteSet.Get(enemy.Facing, enemy.Moving, enemy.Aiming, .55f, simulationTime) : null;
                body.color = enemy.Flash > 0 ? new Color(1f, .94f, .84f) : enemy.Root > 0 ? new Color(.57f, .69f, 1f) : Color.white;
                body.transform.localScale = Vector3.one * displayScale;
            }
            SetShadow(elite ? 58 : 20, elite ? 22 : 8, new Color(accent.r, accent.g, accent.b, .2f));
            SetShield(false, Color.clear, 0, 0);
            SetHealth(elite ? 62 : 28, elite ? 72 : 43, enemy.Hp / Mathf.Max(1, enemy.MaxHp), accent);
            SetLabel(elite ? displayName : string.Empty, elite ? 82 : 0, new Color(.82f, .73f, .51f));
        }

        private void Place(Vector2 position)
        {
            transform.position = WorldProjection.WorldToScene(position);
            if (sortingGroup == null) sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup != null) sortingGroup.sortingOrder = WorldProjection.SortingOrder(position);
        }

        private void SetShadow(float widthPixels, float heightPixels, Color color)
        {
            if (shadow == null) return;
            shadow.enabled = true;
            shadow.color = color;
            shadow.transform.localScale = new Vector3(widthPixels / 128f, heightPixels / 128f, 1);
        }

        private void SetShield(bool visible, Color color, float widthPixels, float heightPixels)
        {
            if (shield == null) return;
            shield.enabled = visible;
            if (!visible) return;
            shield.color = color;
            shield.transform.localPosition = new Vector3(0, .18f, 0);
            shield.transform.localScale = new Vector3(widthPixels / 128f, heightPixels / 128f, 1);
        }

        private void SetHealth(float widthPixels, float risePixels, float ratio, Color color)
        {
            ratio = Mathf.Clamp01(ratio);
            float full = widthPixels / 100f, fill = Mathf.Max(0, widthPixels - 2) / 100f * ratio;
            if (healthBackground != null)
            {
                healthBackground.transform.localPosition = new Vector3(0, risePixels / 100f, 0);
                healthBackground.transform.localScale = new Vector3(full, .05f, 1);
            }
            if (healthFill != null)
            {
                healthFill.enabled = fill > 0;
                healthFill.color = color;
                healthFill.transform.localPosition = new Vector3(-full * .5f + .01f + fill * .5f, risePixels / 100f, 0);
                healthFill.transform.localScale = new Vector3(fill, .03f, 1);
            }
        }

        private void SetLabel(string text, float risePixels, Color color)
        {
            if (nameLabel == null) return;
            nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (string.IsNullOrEmpty(text)) return;
            nameLabel.text = text;
            nameLabel.color = color;
            nameLabel.transform.localPosition = new Vector3(0, risePixels / 100f, 0);
        }

        public void ResetView()
        {
            BoundId = -1;
            LogicalPosition = Vector2.zero;
            if (body != null) { body.color = Color.white; body.sprite = null; }
            if (shield != null) shield.enabled = false;
            if (healthFill != null) healthFill.enabled = true;
            if (nameLabel != null) nameLabel.text = string.Empty;
            if (windupLine != null) windupLine.enabled = false;
            if (empowermentRing != null) empowermentRing.enabled = false;
            if (overloadOrbs != null) foreach (var orb in overloadOrbs) if (orb != null) orb.enabled = false;
        }

#if UNITY_EDITOR
        public void EditorSet(DirectionalSpriteSet frames, SpriteRenderer actorBody, SpriteRenderer actorShadow,
            SpriteRenderer actorShield, SpriteRenderer hpBackground, SpriteRenderer hpFill, TextMesh label,
            float scale, Color color)
        {
            spriteSet = frames; body = actorBody; shadow = actorShadow; shield = actorShield;
            healthBackground = hpBackground; healthFill = hpFill; nameLabel = label;
            displayScale = scale; accent = color;
        }
        public void EditorSetWindup(LineRenderer line) { windupLine = line; }
        public void EditorSetBuffs(LineRenderer ring, SpriteRenderer[] orbs)
        { empowermentRing = ring; overloadOrbs = orbs; }
#endif
    }
}

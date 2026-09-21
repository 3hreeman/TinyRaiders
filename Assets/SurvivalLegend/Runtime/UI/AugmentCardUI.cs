using SurvivalLegend;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalLegend.UI
{
    /// <summary>Saved card presentation. The layout-owned root never moves during reveal.</summary>
    public sealed class AugmentCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button Choose, Reroll;
        public Text Title, Description, Tier;

        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup visualGroup;
        [SerializeField] private Image tierFrame, medallion, ambientGlow;
        [SerializeField] private RawImage iconImage;
        [SerializeField] private Text abilityTag;
        [SerializeField] private AugmentSparkleGraphic sparkles;
        [SerializeField] private Sprite silverFrame, goldFrame, prismFrame;

        private AugmentChoice boundChoice;
        private int boundIndex = -1, boundCount = 1;
        private bool ready, hovered, revealComplete;
        private float revealStart, hoverAmount;
        private Color tierColor = new Color(.75f, .86f, .96f);
        private string tierKey;
        private const float CardWidth = 292f;
        private const float LayoutGap = 16f;
        private const float RevealDuration = .68f;

        public bool RevealComplete => revealComplete;

        public void BindPresentation(AugmentChoice data, Texture2D icon, int index, int count, bool canChoose)
        {
            if (data == null) return;
            if (!ReferenceEquals(boundChoice, data) || boundIndex != index)
            {
                boundChoice = data;
                boundIndex = index;
                boundCount = Mathf.Max(1, count);
                revealStart = Time.unscaledTime;
                revealComplete = false;
                hoverAmount = 0;
                hovered = false;
            }
            else boundCount = Mathf.Max(1, count);
            ready = canChoose;
            if (Title != null) Title.text = data.Title ?? string.Empty;
            if (Description != null) Description.text = data.Description ?? string.Empty;
            if (iconImage != null) { iconImage.texture = icon; iconImage.enabled = icon != null; }
            if (abilityTag != null) abilityTag.text = ResolveTag(data);
            string nextTier = string.IsNullOrEmpty(data.Tier) ? "silver" : data.Tier.ToLowerInvariant();
            if (nextTier != tierKey) ApplyTier(nextTier);
        }

        public void OnPointerEnter(PointerEventData eventData)
        { if (ready && revealComplete) hovered = true; }
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        private void OnEnable()
        {
            boundChoice = null;
            boundIndex = -1;
            revealComplete = false;
            hoverAmount = 0;
            hovered = false;
            revealStart = Time.unscaledTime;
            if (visualRoot != null)
            {
                visualRoot.anchoredPosition = Vector2.zero;
                visualRoot.localScale = Vector3.one * .65f;
                visualRoot.localRotation = Quaternion.identity;
            }
            if (visualGroup != null) { visualGroup.alpha = 0; visualGroup.blocksRaycasts = false; visualGroup.interactable = false; }
        }
        private void OnDisable()
        {
            hovered = false;
            hoverAmount = 0;
            revealComplete = false;
            boundChoice = null;
            if (visualRoot != null)
            {
                visualRoot.anchoredPosition = Vector2.zero;
                visualRoot.localScale = Vector3.one;
                visualRoot.localRotation = Quaternion.identity;
            }
            if (visualGroup != null) { visualGroup.alpha = 1; visualGroup.blocksRaycasts = false; visualGroup.interactable = false; }
        }

        private void Update()
        {
            if (boundChoice == null || visualRoot == null) return;
            float now = Time.unscaledTime;
            float delay = boundIndex * .08f;
            float progress = Mathf.Clamp01((now - revealStart - delay) / RevealDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            revealComplete = progress >= 1f;
            float middle = (boundCount - 1f) * .5f;
            var start = new Vector2((middle - boundIndex) * (CardWidth + LayoutGap), 38f);
            visualRoot.anchoredPosition = Vector2.LerpUnclamped(start, Vector2.zero, eased);
            visualRoot.localRotation = Quaternion.Euler(0, 0, (boundIndex - middle) * 13f * (1f - eased));
            float desiredHover = revealComplete && ready && hovered ? 1f : 0f;
            hoverAmount = Mathf.Lerp(hoverAmount, desiredHover, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 11f));
            visualRoot.localScale = Vector3.one * (Mathf.Lerp(.65f, 1f, eased) * (1f + .045f * hoverAmount));
            if (visualGroup != null)
            {
                visualGroup.alpha = Mathf.Clamp01(progress * 2.7f);
                visualGroup.blocksRaycasts = ready && revealComplete;
                visualGroup.interactable = ready && revealComplete;
            }
            if (ambientGlow != null)
            {
                var glow = tierColor;
                float arrivalPulse = Mathf.Sin(Mathf.PI * progress) *
                    (tierKey == "prism" || tierKey == "special" ? .20f : tierKey == "gold" ? .15f : .10f);
                glow.a = (.11f + .045f * Mathf.Sin(now * 3.1f)) * eased
                    + arrivalPulse + hoverAmount * .09f;
                ambientGlow.color = glow;
            }
            if (sparkles != null) sparkles.SetAnimation(tierKey, now, eased, progress);
        }

        private void ApplyTier(string nextTier)
        {
            tierKey = nextTier;
            bool prism = nextTier == "prism" || nextTier == "special";
            tierColor = prism ? new Color(.72f, .53f, 1f) : nextTier == "gold"
                ? new Color(.91f, .70f, .38f) : new Color(.75f, .86f, .96f);
            if (tierFrame != null)
            {
                tierFrame.sprite = prism ? prismFrame : nextTier == "gold" ? goldFrame : silverFrame;
                tierFrame.type = Image.Type.Sliced;
            }
            if (medallion != null) medallion.color = tierColor;
            if (Tier != null)
            {
                Tier.text = nextTier == "special" ? "특별" : nextTier == "prism" ? "프리즘"
                    : nextTier == "gold" ? "골드" : "실버";
                Tier.color = tierColor;
            }
        }

        private static string ResolveTag(AugmentChoice data)
        {
            if (data.Tier == "special") return "특별 능력";
            if (data.SkillSlot >= 0 && data.SkillSlot < 4) return "스킬 " + "QWER"[data.SkillSlot] + " 강화";
            if (!string.IsNullOrEmpty(data.Upgrade)) return "스킬 강화";
            switch (data.Stat)
            {
                case "damage": return "공격 강화";
                case "armor": return "방어 강화";
                case "speed": return "이동 강화";
                case "attackSpeed": return "공격 속도";
                case "critChance": return "치명타";
                case "healthRegen": return "생명 회복";
                case "lifesteal": return "흡혈";
                case "all": return "균형 가속";
                case "q": return "Q 스킬 가속";
                case "w": return "W 스킬 가속";
                case "e": return "E 스킬 가속";
                default: return "능력 강화";
            }
        }

#if UNITY_EDITOR
        public void EditorSet(RectTransform root, CanvasGroup group, Image frame, Image medal, Image glow,
            RawImage icon, Text tag, AugmentSparkleGraphic particles, Sprite silver, Sprite gold, Sprite prism)
        {
            visualRoot = root; visualGroup = group; tierFrame = frame; medallion = medal;
            ambientGlow = glow; iconImage = icon; abilityTag = tag; sparkles = particles;
            silverFrame = silver; goldFrame = gold; prismFrame = prism;
        }
#endif
    }
}

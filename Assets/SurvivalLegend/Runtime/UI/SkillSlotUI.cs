using UnityEngine;
using UnityEngine.UI;
namespace SurvivalLegend.UI
{
    public sealed class SkillSlotUI : MonoBehaviour
    {
        public Button Cast;
        public RawImage Icon;
        public Text Name, Cooldown, Key, Level;
        public Image CooldownFill;
        public Toggle QuickCast;
    }
}

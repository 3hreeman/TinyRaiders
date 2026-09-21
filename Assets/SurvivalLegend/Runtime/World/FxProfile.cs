using UnityEngine;

namespace SurvivalLegend.World
{
    [CreateAssetMenu(menuName="Survival Legend/FX/Profile")]
    public sealed class FxProfile : ScriptableObject
    {
        public string Id;
        public SkillFxView Prefab;
        public Color Tint=Color.white;
        [Min(.05f)] public float Duration=.65f;
        [Min(.05f)] public float Tail=.6f;
        [Min(.01f)] public float Scale=1;
        public bool ScaleByRadius;
        public bool FollowPlayer;
        public bool SustainWhileBuff;
    }
}

using UnityEngine;

namespace SurvivalLegend.World
{
    [CreateAssetMenu(menuName="Survival Legend/FX/Catalog")]
    public sealed class SkillFxCatalog : ScriptableObject
    {
        public FxProfile[] Profiles=System.Array.Empty<FxProfile>();
        [Range(24,256)] public int MaximumActiveEffects=128;
        public FxProfile Find(string id)
        {foreach(var profile in Profiles)if(profile!=null&&profile.Id==id)return profile;return null;}
    }
}

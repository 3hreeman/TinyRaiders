using System;
using UnityEngine;

namespace SurvivalLegend.World
{
    [Serializable]
    public sealed class ActorVisualDefinition
    {
        public string id;
        public ActorView prefab;
        public DirectionalSpriteSet spriteSet;
        public Color accent = Color.white;
        public float displayScale = .55f;
    }

    /// <summary>Inspector-editable presentation mapping. Missing content IDs use a fallback view.</summary>
    [CreateAssetMenu(menuName = "Survival Legend/World Visual Database")]
    public sealed class WorldVisualDatabase : ScriptableObject
    {
        [SerializeField] private ActorVisualDefinition[] characters = Array.Empty<ActorVisualDefinition>();
        [SerializeField] private ActorVisualDefinition[] enemies = Array.Empty<ActorVisualDefinition>();
        [SerializeField] private ActorVisualDefinition characterFallback;
        [SerializeField] private ActorVisualDefinition enemyFallback;
        [SerializeField] private ProjectileView projectilePrefab;
        [SerializeField] private ZoneView zonePrefab;
        [SerializeField] private WorldEffectView effectPrefab;
        [SerializeField] private AimMarkerView aimMarkerPrefab;

        public ProjectileView ProjectilePrefab => projectilePrefab;
        public ZoneView ZonePrefab => zonePrefab;
        public WorldEffectView EffectPrefab => effectPrefab;
        public AimMarkerView AimMarkerPrefab => aimMarkerPrefab;

        public ActorVisualDefinition Character(string id) => Find(characters, id) ?? characterFallback ?? First(characters);
        public ActorVisualDefinition Enemy(string id) => Find(enemies, id) ?? enemyFallback ?? First(enemies);

        private static ActorVisualDefinition Find(ActorVisualDefinition[] list, string id)
        {
            if (list == null) return null;
            foreach (ActorVisualDefinition item in list) if (item != null && item.id == id) return item;
            return null;
        }
        private static ActorVisualDefinition First(ActorVisualDefinition[] list)
        {
            if (list == null) return null;
            foreach (ActorVisualDefinition item in list) if (item != null && item.prefab != null) return item;
            return null;
        }

#if UNITY_EDITOR
        public void EditorSet(ActorVisualDefinition[] playerEntries, ActorVisualDefinition[] enemyEntries,
            ProjectileView projectile, ZoneView zone, WorldEffectView effect, AimMarkerView aim)
        {
            if (characters == null || characters.Length == 0) characters = playerEntries;
            if (enemies == null || enemies.Length == 0) enemies = enemyEntries;
            if (characterFallback == null) characterFallback = First(playerEntries);
            if (enemyFallback == null) enemyFallback = First(enemyEntries);
            if (projectilePrefab == null) projectilePrefab = projectile;
            if (zonePrefab == null) zonePrefab = zone;
            if (effectPrefab == null) effectPrefab = effect;
            if (aimMarkerPrefab == null) aimMarkerPrefab = aim;
        }
#endif
    }
}

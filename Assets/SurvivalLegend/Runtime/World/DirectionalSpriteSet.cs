using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>Editor-sliced SD sheet: eight world facings by idle, two walks, three attacks.</summary>
    [CreateAssetMenu(menuName = "Survival Legend/Directional Sprite Set")]
    public sealed class DirectionalSpriteSet : ScriptableObject
    {
        public const int Directions = 8;
        public const int FramesPerDirection = 6;
        [SerializeField] private Sprite[] frames = new Sprite[Directions * FramesPerDirection];
        [SerializeField, Range(.1f, 30f)] private float stride = 9f;
        public float Stride => stride;

        public Sprite Get(float facing, bool moving, bool attacking, float attackProgress, float simulationTime)
        {
            int direction = Mathf.RoundToInt(Mathf.Repeat(WorldProjection.AtlasFacing(facing), Mathf.PI * 2f) / (Mathf.PI / 4f)) & 7;
            int frame = attacking
                ? 3 + Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(attackProgress) * 3f), 0, 2)
                : moving ? 1 + (Mathf.FloorToInt(simulationTime * stride) & 1) : 0;
            int index = direction * FramesPerDirection + frame;
            if (frames == null || index >= frames.Length) return null;
            return frames[index] != null ? frames[index] : frames[direction * FramesPerDirection];
        }

#if UNITY_EDITOR
        public void EditorSet(Sprite[] sprites, float animationStride)
        {
            frames = sprites;
            stride = animationStride;
        }
#endif
    }
}

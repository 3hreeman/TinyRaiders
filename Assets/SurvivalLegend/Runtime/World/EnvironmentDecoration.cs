using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalLegend.World
{
    /// <summary>Editable map prop. Its foot transform is the logical obstacle centre.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class EnvironmentDecoration : MonoBehaviour
    {
        [SerializeField, Min(0)] private float radius;
        [SerializeField] private bool blocksMovement;
        [SerializeField] private string decorationId;
        private SortingGroup sortingGroup;

        public string Id => string.IsNullOrEmpty(decorationId) ? name : decorationId;
        public Vector2 LogicalPosition => WorldProjection.SceneToWorld(transform.position);
        public float Radius => radius;
        public bool BlocksMovement => blocksMovement && radius > 0;

#if UNITY_EDITOR
        public void EditorSet(string id, float obstacleRadius, bool blocks)
        {
            decorationId = id;
            radius = Mathf.Max(0, obstacleRadius);
            blocksMovement = blocks;
            RefreshSorting();
        }
#endif

        private void OnEnable() => RefreshSorting();
        private void OnValidate() { radius = Mathf.Max(0, radius); RefreshSorting(); }
        private void Update()
        {
            if (Application.isPlaying || !transform.hasChanged) return;
            RefreshSorting();
            transform.hasChanged = false;
        }
        private void RefreshSorting()
        {
            if (sortingGroup == null) sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup != null) sortingGroup.sortingOrder = WorldProjection.SortingOrder(LogicalPosition);
        }
        private void OnDrawGizmosSelected()
        {
            if (!BlocksMovement) return;
            Gizmos.color = new Color(.96f, .62f, .30f, .9f);
            var center = LogicalPosition;
            const int segments = 40;
            Vector3 previous = WorldProjection.WorldToScene(center + Vector2.right * radius);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector3 next = WorldProjection.WorldToScene(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}

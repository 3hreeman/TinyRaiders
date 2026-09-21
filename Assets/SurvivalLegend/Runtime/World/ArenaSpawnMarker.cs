using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>Opt-in logical enemy spawn marker under ArenaMap/SpawnMarkers.</summary>
    [ExecuteAlways]
    public sealed class ArenaSpawnMarker : MonoBehaviour
    {
        [SerializeField] private Vector2 logicalPosition = new Vector2(720, 720);
        public Vector2 LogicalPosition => logicalPosition;
        private void OnValidate() => transform.localPosition = WorldProjection.WorldToScene(logicalPosition);
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, .77f, .47f, .9f);
            Gizmos.DrawWireSphere(transform.position, .12f);
        }
    }
}

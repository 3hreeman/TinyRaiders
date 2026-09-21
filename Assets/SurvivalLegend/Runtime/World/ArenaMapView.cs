using System;
using System.Collections.Generic;
using SurvivalLegend.Data;
using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>Saved map layers and world-space authoring markers; simulation owns collision.</summary>
    public sealed class ArenaMapView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backdrop;
        [SerializeField] private Transform ground;
        [SerializeField] private Transform floorTiles;
        [SerializeField] private Transform decorations;
        [SerializeField] private Transform bounds;
        [SerializeField] private Transform spawnMarkers;
        [SerializeField] private Vector2 logicalSize = new Vector2(1440,1440);
        public Vector2 LogicalSize => logicalSize;
        public Transform Ground => ground;
        public Transform FloorTiles => floorTiles;
        public Transform Decorations => decorations;
        public Transform Bounds => bounds;
        public Transform SpawnMarkers => spawnMarkers;

        public GameContentSnapshot BuildSnapshot(GameContentSnapshot source)
        {
            if (source == null) return null;
            source=source.WithArena(logicalSize.x,logicalSize.y,source.Config.ArenaInset,source.SpawnPoints);
            var points = new List<Vector2>();
            if(spawnMarkers!=null) foreach (var marker in spawnMarkers.GetComponentsInChildren<ArenaSpawnMarker>(true))
            {
                // Do not use isActiveAndEnabled: the title may hide world actors while
                // authoring data must still be available for StartRun.
                if (!marker.enabled || !AuthoringActive(marker.transform)) continue;
                Vector2 point = marker.LogicalPosition;
                if (float.IsNaN(point.x) || float.IsInfinity(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.y)
                    || point.x < 0 || point.y < 0 || point.x > source.Config.ArenaWidth || point.y > source.Config.ArenaHeight)
                    throw new ArgumentOutOfRangeException(nameof(spawnMarkers),
                        $"Spawn marker '{marker.name}' must have finite logical coordinates inside the arena.");
                points.Add(point);
            }
            var snapshot=points.Count == 0 ? source : source.WithArena(source.Config.ArenaWidth,
                source.Config.ArenaHeight, source.Config.ArenaInset, points.ToArray());
            var obstacles=new List<ObstacleData>();
            foreach(var prop in GetComponentsInChildren<EnvironmentDecoration>(true))
            {
                if(!prop.enabled||!prop.BlocksMovement||!AuthoringActive(prop.transform))continue;
                var p=prop.LogicalPosition;float radius=prop.Radius;
                if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)
                    ||float.IsNaN(radius)||float.IsInfinity(radius)||radius<=0
                    ||p.x<0||p.y<0||p.x>source.Config.ArenaWidth||p.y>source.Config.ArenaHeight)
                    throw new ArgumentOutOfRangeException(nameof(decorations),$"Obstacle '{prop.name}' needs a finite position inside the arena and a positive radius.");
                obstacles.Add(new ObstacleData(prop.name+"-"+obstacles.Count,p,radius));
            }
            return snapshot.WithObstacles(obstacles);
        }

        bool AuthoringActive(Transform child)
        {
            for(var current=child;current!=null&&current!=transform;current=current.parent)
                if(!current.gameObject.activeSelf)return false;
            return true;
        }

#if UNITY_EDITOR
        public void EditorSetSize(Vector2 size) { logicalSize=size; }
        public void EditorSet(SpriteRenderer backdropRenderer, Transform groundLayer, Transform floorLayer,
            Transform decorationLayer, Transform boundsLayer, Transform spawns)
        {
            backdrop = backdropRenderer; ground = groundLayer; floorTiles = floorLayer;
            decorations = decorationLayer; bounds = boundsLayer; spawnMarkers = spawns;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.71f, .95f, .62f, .8f);
            var content = SurvivorGame.Instance != null ? SurvivorGame.Instance.Content : null;
            float width = logicalSize.x;
            float height = logicalSize.y;
            Vector2 a = Vector2.zero, b = new Vector2(width, 0);
            Vector2 c = new Vector2(width, height), d = new Vector2(0, height);
            Gizmos.DrawLine(WorldProjection.WorldToScene(a), WorldProjection.WorldToScene(b));
            Gizmos.DrawLine(WorldProjection.WorldToScene(b), WorldProjection.WorldToScene(c));
            Gizmos.DrawLine(WorldProjection.WorldToScene(c), WorldProjection.WorldToScene(d));
            Gizmos.DrawLine(WorldProjection.WorldToScene(d), WorldProjection.WorldToScene(a));
            if (spawnMarkers == null) return;
            Gizmos.color = new Color(1f, .77f, .47f, .9f);
            foreach (Transform marker in spawnMarkers) if (marker != null) Gizmos.DrawWireSphere(marker.position, .12f);
        }
    }

}

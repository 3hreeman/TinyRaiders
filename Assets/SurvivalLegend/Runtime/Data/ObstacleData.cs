using System;
using UnityEngine;

namespace SurvivalLegend.Data
{
    /// <summary>Solid logical-space disk; overlapping disks form a union.</summary>
    [Serializable]
    public readonly struct ObstacleData
    {
        public readonly string Id;
        public readonly Vector2 Position;
        public readonly float Radius;
        public ObstacleData(string id,Vector2 position,float radius){Id=id;Position=position;Radius=radius;}
    }
}

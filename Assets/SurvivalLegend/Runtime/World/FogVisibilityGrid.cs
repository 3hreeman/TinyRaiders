using System;
using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>Player-centred visibility with persistent exploration, independent of the camera.</summary>
    public sealed class FogVisibilityGrid
    {
        public Vector2 LogicalSize { get; }
        public int Width { get; }
        public int Height { get; }
        readonly bool[] explored;
        Vector2 center;
        float radius;
        bool revealed;
        public FogVisibilityGrid(Vector2 logicalSize,int resolution=192)
        {
            if(logicalSize.x<=0||logicalSize.y<=0||float.IsNaN(logicalSize.x)||float.IsNaN(logicalSize.y)
                ||float.IsInfinity(logicalSize.x)||float.IsInfinity(logicalSize.y))throw new ArgumentException("Fog needs finite positive map dimensions.");
            LogicalSize=logicalSize;Width=Mathf.Clamp(resolution,16,512);
            Height=Mathf.Clamp(Mathf.RoundToInt(Width*logicalSize.y/logicalSize.x),16,512);
            explored=new bool[Width*Height];
        }
        public void Clear(){Array.Clear(explored,0,explored.Length);revealed=false;radius=0;}
        bool Inside(Vector2 p)=>p.x>=0&&p.y>=0&&p.x<=LogicalSize.x&&p.y<=LogicalSize.y;
        int Index(Vector2 p)=>Mathf.Clamp((int)(p.y/LogicalSize.y*Height),0,Height-1)*Width+Mathf.Clamp((int)(p.x/LogicalSize.x*Width),0,Width-1);
        public bool IsVisible(Vector2 p)=>revealed&&Inside(p)&&(p-center).sqrMagnitude<=radius*radius;
        public bool IsExplored(Vector2 p)=>Inside(p)&&(IsVisible(p)||explored[Index(p)]);
        public void Reveal(Vector2 point,float visionRadius)
        {
            if(float.IsNaN(point.x)||float.IsInfinity(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.y)
                ||visionRadius<=0||float.IsNaN(visionRadius)||float.IsInfinity(visionRadius))throw new ArgumentException("Fog reveal needs finite coordinates and positive radius.");
            center=point;radius=visionRadius;revealed=true;
            int left=Mathf.Clamp(Mathf.FloorToInt((point.x-radius)/LogicalSize.x*Width),0,Width-1);
            int right=Mathf.Clamp(Mathf.CeilToInt((point.x+radius)/LogicalSize.x*Width),0,Width-1);
            int top=Mathf.Clamp(Mathf.FloorToInt((point.y-radius)/LogicalSize.y*Height),0,Height-1);
            int bottom=Mathf.Clamp(Mathf.CeilToInt((point.y+radius)/LogicalSize.y*Height),0,Height-1);
            for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)
                if((CellCenter(x,y)-point).sqrMagnitude<=radius*radius)explored[y*Width+x]=true;
        }
        Vector2 CellCenter(int x,int y)=>new Vector2((x+.5f)*LogicalSize.x/Width,(y+.5f)*LogicalSize.y/Height);
        public float GetAlpha(int x,int y)
        {
            if(x<0||x>=Width||y<0||y>=Height)return 1;
            float memory=explored[y*Width+x]?.72f:1;
            if(!revealed)return memory;
            float distance=Vector2.Distance(CellCenter(x,y),center);
            if(distance>=radius)return memory;
            return Mathf.SmoothStep(0,memory,Mathf.InverseLerp(radius*.82f,radius,distance));
        }
    }
}

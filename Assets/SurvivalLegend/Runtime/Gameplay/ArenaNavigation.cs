using System;
using System.Collections.Generic;
using SurvivalLegend.Data;
using UnityEngine;

namespace SurvivalLegend
{
    /// <summary>Static clearance grids and shared reverse-Dijkstra fields; never uses gameplay RNG.</summary>
    public sealed class ArenaNavigation
    {
        const float CellSize=32, Skin=.04f;
        static readonly int[] Dx={1,0,-1,0,1,-1,-1,1};
        static readonly int[] Dy={0,1,0,-1,1,1,-1,-1};
        readonly GameContentSnapshot content;
        readonly ObstacleData[] obstacles;
        readonly Dictionary<int,Grid> grids=new Dictionary<int,Grid>();
        long access;
        public bool HasObstacles=>content.Obstacles.Count>0;
        public int FieldBuildCount {get;private set;}
        public ArenaNavigation(GameContentSnapshot snapshot){content=snapshot??throw new ArgumentNullException(nameof(snapshot));obstacles=new ObstacleData[content.Obstacles.Count];for(int i=0;i<obstacles.Length;i++)obstacles[i]=content.Obstacles[i];}

        Vector2 Clamp(Vector2 point,float radius)
        {
            float inset=Mathf.Max(content.Config.ArenaInset,radius);
            return new Vector2(Mathf.Clamp(point.x,inset,content.Config.ArenaWidth-inset),Mathf.Clamp(point.y,inset,content.Config.ArenaHeight-inset));
        }
        public bool IsWalkable(Vector2 point,float radius)
        {
            if(!Finite(point)||float.IsNaN(radius)||float.IsInfinity(radius)||radius<0)return false;
            float inset=Mathf.Max(content.Config.ArenaInset,radius);
            if(point.x<inset-.001f||point.y<inset-.001f||point.x>content.Config.ArenaWidth-inset+.001f||point.y>content.Config.ArenaHeight-inset+.001f)return false;
            foreach(var solid in obstacles)if((point-solid.Position).sqrMagnitude<(solid.Radius+radius+Skin)*(solid.Radius+radius+Skin)-.0001f)return false;
            return true;
        }
        static bool Finite(Vector2 point)=>!float.IsNaN(point.x)&&!float.IsInfinity(point.x)&&!float.IsNaN(point.y)&&!float.IsInfinity(point.y);
        public Vector2 ResolvePosition(Vector2 point,float radius)
        {
            if(!Finite(point)||float.IsNaN(radius)||float.IsInfinity(radius)||radius<0)throw new ArgumentException("Navigation needs finite coordinates and nonnegative radius.");
            point=Clamp(point,radius);if(IsWalkable(point,radius))return point;
            Vector2 requested=point;
            for(int pass=0;pass<16;pass++)
            {
                foreach(var solid in obstacles)
                {
                    Vector2 offset=point-solid.Position;float minimum=solid.Radius+radius+Skin+.01f;
                    if(offset.sqrMagnitude>=minimum*minimum)continue;
                    point=Clamp(solid.Position+(offset.sqrMagnitude>.00001f?offset.normalized:Vector2.right)*minimum,radius);
                }
                if(IsWalkable(point,radius))return point;
            }
            var grid=GetGrid(radius);int best=-1;float distance=float.MaxValue;
            for(int i=0;i<grid.Open.Length;i++)if(grid.Open[i]){float d=(grid.Point(i)-requested).sqrMagnitude;if(d<distance){distance=d;best=i;}}
            if(best<0)throw new InvalidOperationException("The arena has no free position for actor radius "+radius+".");
            return grid.Point(best);
        }
        public bool HasLineOfSight(Vector2 from,Vector2 to,float radius)
        {
            if(!IsWalkable(from,radius)||!IsWalkable(to,radius))return false;
            Vector2 delta=to-from;float length=delta.sqrMagnitude;
            foreach(var solid in obstacles)
            {
                float t=length>.00001f?Mathf.Clamp01(Vector2.Dot(solid.Position-from,delta)/length):0;
                float r=solid.Radius+radius+Skin;
                if((from+delta*t-solid.Position).sqrMagnitude<r*r)return false;
            }
            return true;
        }
        /// <summary>Continuous collision sweep, including dashes/teleports. Stops before the first solid.</summary>
        public Vector2 SweepMove(Vector2 from,Vector2 to,float radius)
        {
            from=ResolvePosition(from,radius);to=Clamp(to,radius);
            Vector2 delta=to-from;float a=delta.sqrMagnitude;if(a<.0000001f)return from;
            float fraction=1;
            foreach(var solid in obstacles)
            {
                Vector2 offset=from-solid.Position;float r=solid.Radius+radius+Skin;
                float b=Vector2.Dot(offset,delta),c=offset.sqrMagnitude-r*r;
                float discriminant=b*b-a*c;if(discriminant<0)continue;
                float contact=(-b-Mathf.Sqrt(discriminant))/a;
                if(contact>=0&&contact<fraction)fraction=contact;
            }
            if(fraction<1)fraction=Mathf.Max(0,fraction-.01f/Mathf.Sqrt(a));
            return from+delta*fraction;
        }
        /// <summary>Nearest safe perimeter placement; uses no random draws and accounts for actor radius.</summary>
        public Vector2 ResolveEdgePosition(Vector2 requested,float radius)
        {
            float inset=Mathf.Max(content.Config.ArenaInset,radius),maxX=content.Config.ArenaWidth-inset,maxY=content.Config.ArenaHeight-inset;
            Vector2 best=Vector2.zero;float distance=float.MaxValue;
            void Consider(Vector2 point){float d=(point-requested).sqrMagnitude;if(d<distance&&IsWalkable(point,radius)){distance=d;best=point;}}
            float x=Mathf.Clamp(requested.x,inset,maxX),y=Mathf.Clamp(requested.y,inset,maxY);
            Consider(new Vector2(inset,y));Consider(new Vector2(maxX,y));Consider(new Vector2(x,inset));Consider(new Vector2(x,maxY));
            int nx=Mathf.Max(1,Mathf.CeilToInt((maxX-inset)/CellSize)),ny=Mathf.Max(1,Mathf.CeilToInt((maxY-inset)/CellSize));
            for(int i=0;i<=nx;i++){float along=Mathf.Lerp(inset,maxX,i/(float)nx);Consider(new Vector2(along,inset));Consider(new Vector2(along,maxY));}
            for(int i=0;i<=ny;i++){float along=Mathf.Lerp(inset,maxY,i/(float)ny);Consider(new Vector2(inset,along));Consider(new Vector2(maxX,along));}
            if(distance==float.MaxValue)throw new InvalidOperationException("The arena boundary has no safe spawn for actor radius "+radius+".");
            return best;
        }
        public Vector2 NextWaypoint(Vector2 from,Vector2 target,float radius)
        {
            from=ResolvePosition(from,radius);target=ResolvePosition(target,radius);
            if(HasLineOfSight(from,target,radius))return target;
            var grid=GetGrid(radius);int goal=ClosestOpen(grid,target,radius);if(goal<0)return from;
            var field=GetField(grid,goal);int start=Connector(grid,field,from,radius);
            if(start<0)return from;
            int current=start;
            // String-pull the shared route so motion does not visibly zigzag at every grid cell.
            Vector2 waypoint=grid.Point(current);
            for(int steps=0;steps<10;steps++)
            {
                int next=field.Next[current];if(next<0)break;
                var point=grid.Point(next);if(!HasLineOfSight(from,point,radius))break;
                waypoint=point;current=next;
            }
            if(current==goal&&HasLineOfSight(from,target,radius))return target;
            return waypoint;
        }
        int Connector(Grid grid,Field field,Vector2 from,float radius)
        {
            int x=Mathf.Clamp(Mathf.FloorToInt(from.x/CellSize),0,grid.Width-1),y=Mathf.Clamp(Mathf.FloorToInt(from.y/CellSize),0,grid.Height-1);
            int own=y*grid.Width+x;
            if(grid.Open[own]&&field.Distance[own]!=int.MaxValue&&HasLineOfSight(from,grid.Point(own),radius))return own;
            int best=-1;float score=float.MaxValue;
            for(int oy=-3;oy<=3;oy++)for(int ox=-3;ox<=3;ox++)
            {
                int cx=x+ox,cy=y+oy;if(cx<0||cy<0||cx>=grid.Width||cy>=grid.Height)continue;
                int index=cy*grid.Width+cx;if(!grid.Open[index]||field.Distance[index]==int.MaxValue)continue;
                Vector2 point=grid.Point(index);if(!HasLineOfSight(from,point,radius))continue;
                float value=field.Distance[index]+Vector2.Distance(from,point)*10/CellSize;
                if(value<score){score=value;best=index;}
            }
            return best;
        }
        int ClosestOpen(Grid grid,Vector2 point,float radius)
        {
            int cell=Mathf.Clamp(Mathf.FloorToInt(point.y/CellSize),0,grid.Height-1)*grid.Width+Mathf.Clamp(Mathf.FloorToInt(point.x/CellSize),0,grid.Width-1);
            if(grid.Open[cell]&&HasLineOfSight(point,grid.Point(cell),radius))return cell;
            int best=-1;float distance=float.MaxValue;
            for(int i=0;i<grid.Open.Length;i++)if(grid.Open[i]){float d=(grid.Point(i)-point).sqrMagnitude;if(d<distance&&HasLineOfSight(point,grid.Point(i),radius)){distance=d;best=i;}}
            return best;
        }
        Grid GetGrid(float radius)
        {
            int bucket=Mathf.CeilToInt(radius/4);if(grids.TryGetValue(bucket,out var grid))return grid;
            grid=new Grid(Mathf.CeilToInt(content.Config.ArenaWidth/CellSize),Mathf.CeilToInt(content.Config.ArenaHeight/CellSize),bucket*4);
            for(int i=0;i<grid.Open.Length;i++)grid.Open[i]=IsWalkable(grid.Point(i),grid.Radius);
            for(int i=0;i<grid.Open.Length;i++)
            {
                if(!grid.Open[i])continue;int x=i%grid.Width,y=i/grid.Width;
                for(int direction=0;direction<8;direction++)
                {
                    int nx=x+Dx[direction],ny=y+Dy[direction];if(nx<0||ny<0||nx>=grid.Width||ny>=grid.Height)continue;
                    int neighbor=ny*grid.Width+nx;
                    if(grid.Open[neighbor]&&HasLineOfSight(grid.Point(i),grid.Point(neighbor),grid.Radius))grid.Edges[i]|=(byte)(1<<direction);
                }
            }
            grids.Add(bucket,grid);return grid;
        }
        Field GetField(Grid grid,int goal)
        {
            foreach(var cached in grid.Fields)if(cached.Goal==goal){cached.Access=++access;return cached;}
            var field=new Field(goal,grid.Open.Length){Access=++access};var heap=new MinHeap();field.Distance[goal]=0;heap.Push(goal,0);
            while(heap.Count>0)
            {
                heap.Pop(out int node,out int cost);if(cost!=field.Distance[node])continue;
                int x=node%grid.Width,y=node/grid.Width;
                for(int direction=0;direction<8;direction++)
                {
                    if((grid.Edges[node]&(1<<direction))==0)continue;
                    int next=(y+Dy[direction])*grid.Width+x+Dx[direction],distance=cost+(direction<4?10:14);
                    if(distance>=field.Distance[next])continue;
                    field.Distance[next]=distance;field.Next[next]=node;heap.Push(next,distance);
                }
            }
            if(grid.Fields.Count>=4){int oldest=0;for(int i=1;i<grid.Fields.Count;i++)if(grid.Fields[i].Access<grid.Fields[oldest].Access)oldest=i;grid.Fields.RemoveAt(oldest);}
            grid.Fields.Add(field);FieldBuildCount++;return field;
        }
        sealed class Grid
        {
            public readonly int Width,Height;public readonly float Radius;public readonly bool[] Open;public readonly byte[] Edges;public readonly List<Field> Fields=new List<Field>();
            public Grid(int width,int height,float radius){Width=width;Height=height;Radius=radius;Open=new bool[width*height];Edges=new byte[Open.Length];}
            public Vector2 Point(int index)=>new Vector2((index%Width+.5f)*CellSize,(index/Width+.5f)*CellSize);
        }
        sealed class Field
        {
            public readonly int Goal;public readonly int[] Distance,Next;public long Access;
            public Field(int goal,int count){Goal=goal;Distance=new int[count];Next=new int[count];for(int i=0;i<count;i++){Distance[i]=int.MaxValue;Next[i]=-1;}}
        }
        sealed class MinHeap
        {
            readonly List<Vector2Int> items=new List<Vector2Int>();public int Count=>items.Count;
            static bool Less(Vector2Int a,Vector2Int b)=>a.y<b.y||(a.y==b.y&&a.x<b.x);
            public void Push(int node,int cost){var value=new Vector2Int(node,cost);items.Add(value);int i=items.Count-1;while(i>0){int parent=(i-1)/2;if(!Less(value,items[parent]))break;items[i]=items[parent];i=parent;}items[i]=value;}
            public void Pop(out int node,out int cost)
            {
                var first=items[0];node=first.x;cost=first.y;var value=items[items.Count-1];items.RemoveAt(items.Count-1);if(items.Count==0)return;
                int i=0;while(i*2+1<items.Count){int child=i*2+1;if(child+1<items.Count&&Less(items[child+1],items[child]))child++;if(!Less(items[child],value))break;items[i]=items[child];i=child;}items[i]=value;
            }
        }
    }
}

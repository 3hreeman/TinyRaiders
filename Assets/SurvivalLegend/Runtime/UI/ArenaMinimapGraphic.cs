using SurvivalLegend.World;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalLegend.UI
{
    /// <summary>Small allocation-free map markers, separated across fog and overlay layers.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArenaMinimapGraphic : MaskableGraphic
    {
        [SerializeField] ArenaMinimapUI minimap;
        [SerializeField] bool overlay;
        public void Bind(ArenaMinimapUI owner,bool drawOverlay)
        {minimap=owner;overlay=drawOverlay;raycastTarget=false;SetVerticesDirty();}
        public void Refresh(){if(minimap!=null)SetVerticesDirty();}

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if(minimap==null)return;
            Vector2 size=minimap.LogicalSize;
            if(size.x<=0||size.y<=0)return;
            Rect area=rectTransform.rect;
            var run=minimap.Game!=null && minimap.Game.Simulation!=null?minimap.Game.State:null;
            if(!overlay)
            {
                var obstacles=minimap.Obstacles;
                if(obstacles!=null)foreach(var prop in obstacles)
                {
                    if(prop==null||!prop.enabled||!prop.BlocksMovement)continue;
                    var p=prop.LogicalPosition;
                    if(minimap.Fog!=null&&!minimap.Fog.IsExplored(p))continue;
                    Vector2 center=Point(p,size,area);
                    float r=Mathf.Clamp(prop.Radius/size.x*area.width,1.7f,4f);
                    Disc(helper,center,r,new Color32(178,182,147,210));
                }
                if(run!=null)foreach(var enemy in run.Enemies)
                {
                    if(enemy.Hp<=0 || minimap.Fog!=null&&!minimap.Fog.IsVisible(enemy.Position))continue;
                    Disc(helper,Point(enemy.Position,size,area),enemy.Kind=="elite"?4f:2.4f,
                        enemy.Kind=="elite"?new Color32(251,121,70,255):new Color32(213,78,71,245));
                }
                return;
            }
            if(minimap.CameraController!=null)
            {
                Rect logical=minimap.CameraController.VisibleLogicalRect;
                Vector2 topLeft=Point(new Vector2(logical.xMin,logical.yMin),size,area);
                Vector2 bottomRight=Point(new Vector2(logical.xMax,logical.yMax),size,area);
                float left=Mathf.Clamp(topLeft.x,area.xMin,area.xMax),right=Mathf.Clamp(bottomRight.x,area.xMin,area.xMax);
                float top=Mathf.Clamp(topLeft.y,area.yMin,area.yMax),bottom=Mathf.Clamp(bottomRight.y,area.yMin,area.yMax);
                Color outline=new Color32(235,214,156,220);
                Quad(helper,new Rect(left,top-1,right-left,1),outline);
                Quad(helper,new Rect(left,bottom,right-left,1),outline);
                Quad(helper,new Rect(left,bottom,1,top-bottom),outline);
                Quad(helper,new Rect(right-1,bottom,1,top-bottom),outline);
            }
            if(run!=null)Disc(helper,Point(run.Player.Position,size,area),4.2f,new Color32(255,236,155,255));
        }
        static Vector2 Point(Vector2 p,Vector2 size,Rect area)
        {return new Vector2(area.xMin+Mathf.Clamp01(p.x/size.x)*area.width,
            area.yMax-Mathf.Clamp01(p.y/size.y)*area.height);}
        static void Quad(VertexHelper helper,Rect rect,Color color)
        {
            if(rect.width<=0||rect.height<=0)return;
            int i=helper.currentVertCount;
            helper.AddVert(new Vector3(rect.xMin,rect.yMin),color,Vector2.zero);
            helper.AddVert(new Vector3(rect.xMin,rect.yMax),color,Vector2.up);
            helper.AddVert(new Vector3(rect.xMax,rect.yMax),color,Vector2.one);
            helper.AddVert(new Vector3(rect.xMax,rect.yMin),color,Vector2.right);
            helper.AddTriangle(i,i+1,i+2);helper.AddTriangle(i,i+2,i+3);
        }
        static void Disc(VertexHelper helper,Vector2 center,float radius,Color color)
        {
            int i=helper.currentVertCount;
            helper.AddVert(center,color,new Vector2(.5f,.5f));
            const int segments=8;
            for(int n=0;n<=segments;n++)
            {
                float a=n*Mathf.PI*2/segments;
                helper.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);
                if(n>0)helper.AddTriangle(i,i+n,i+n+1);
            }
        }
    }
}

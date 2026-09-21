using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>One reversible mapping for authored world renderers, camera and pointer input.</summary>
    public static class WorldProjection
    {
        public const float ViewWidth=1280,ViewHeight=720,PixelsPerUnit=100;
        // Authored world scale is independent of the camera's display area.
        public const float FieldLeft=40,FieldTop=45,FieldWidth=1200,FieldHeight=470;
        // Extend the world down to the canvas bottom, behind the overlay HUD.
        public const float CameraViewportHeight=ViewHeight-FieldTop;
        public const float CameraAspect=FieldWidth/CameraViewportHeight;
        public const float DefaultCameraSize=CameraViewportHeight/PixelsPerUnit/2;
        public const float HorizontalScale=FieldWidth/1440f,VerticalScale=FieldHeight/1440f;
        public static Vector2 WorldToView(Vector2 point,float elevation=0)
            =>new Vector2(FieldLeft+point.x*HorizontalScale,FieldTop+point.y*VerticalScale-elevation);
        public static Vector2 ViewToWorld(Vector2 view)
        {
            return new Vector2((view.x-FieldLeft)/HorizontalScale,(view.y-FieldTop)/VerticalScale);
        }
        public static Vector2 DirectionToScene(Vector2 direction)
            =>new Vector2(direction.x*HorizontalScale,-direction.y*VerticalScale);
        // Existing eight-direction sheets were authored against the original diamond axes.
        public static float AtlasFacing(float logicalFacing)
        {
            float dx=Mathf.Cos(logicalFacing)*HorizontalScale/.39f;
            float dy=Mathf.Sin(logicalFacing)*VerticalScale/.155f;
            return Mathf.Atan2(dy-dx,dy+dx);
        }
        public static Vector3 WorldToScene(Vector2 point,float elevation=0)
        {
            var view=WorldToView(point,elevation);
            return new Vector3((view.x-640)/PixelsPerUnit,(360-view.y)/PixelsPerUnit,0);
        }
        public static Vector2 SceneToWorld(Vector3 scene)=>ViewToWorld(new Vector2(scene.x*PixelsPerUnit+640,360-scene.y*PixelsPerUnit));
        public static Vector2 ScreenToWorld(Camera camera,Vector2 screen)
        {
            if(camera!=null)
            {
                var ray=camera.ScreenPointToRay(screen);
                float distance=Mathf.Abs(ray.direction.z)>.000001f?-ray.origin.z/ray.direction.z:0;
                return SceneToWorld(ray.origin+ray.direction*distance);
            }
            var rect=ViewportPixels(Screen.width,Screen.height);
            return ViewToWorld(new Vector2((screen.x-rect.x)/rect.width*ViewWidth,ViewHeight-(screen.y-rect.y)/rect.height*ViewHeight));
        }
        public static Rect ViewportPixels(float width,float height)
        {
            float scale=Mathf.Min(width/ViewWidth,height/ViewHeight);
            return new Rect((width-ViewWidth*scale)/2,(height-ViewHeight*scale)/2,ViewWidth*scale,ViewHeight*scale);
        }
        public static void ConfigureCamera(Camera camera)
        {
            if(camera==null)return;
            ConfigureViewport(camera);camera.orthographicSize=DefaultCameraSize;
            var center=WorldToScene(new Vector2(720,720));center.z=-10;
            camera.transform.position=center;camera.transform.rotation=Quaternion.identity;
        }
        public static Rect FieldViewportPixels(float width,float height)
        {
            var canvas=ViewportPixels(width,height);float scale=canvas.width/ViewWidth;
            return new Rect(canvas.x+FieldLeft*scale,canvas.y,FieldWidth*scale,CameraViewportHeight*scale);
        }
        public static void ConfigureViewport(Camera camera)
        {
            if(camera==null)return;
            float width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);var rect=FieldViewportPixels(width,height);
            camera.orthographic=true;camera.aspect=CameraAspect;
            camera.rect=new Rect(rect.x/width,rect.y/height,rect.width/width,rect.height/height);
        }
        public static int SortingOrder(Vector2 logical)=>Mathf.RoundToInt(Mathf.Clamp(logical.y,0,8000));
    }
}

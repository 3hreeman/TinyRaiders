using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SurvivalLegend.World
{
    /// <summary>Presentation-only camera. Logical projection and simulation positions never change with zoom.</summary>
    [DefaultExecutionOrder(150)]
    public sealed class ArenaCameraController : MonoBehaviour
    {
        [SerializeField] SurvivorGame game;
        [SerializeField] Camera worldCamera;
        [Header("Orthographic size in scene units")]
        [Min(.1f)] public float DefaultOrthographicSize=WorldProjection.DefaultCameraSize;
        [Min(.1f)] public float MinimumOrthographicSize=WorldProjection.DefaultCameraSize*.5f;
        [Min(.1f)] public float MaximumOrthographicSize=5.875f;
        [Header("Pointer navigation")]
        [Min(1)] public float EdgeBandPixels=18;
        [Min(0)] public float PanUnitsPerSecond=7;
        [Range(.01f,.5f)] public float WheelZoomRate=.12f;
        readonly List<RaycastResult> hits=new List<RaycastResult>();
        PointerEventData pointer;
        EventSystem pointerSystem;
        int generation=-1;
        public Camera Camera=>worldCamera;
        public bool IsFollowing {get;private set;}
        public bool InputAllowed=>game!=null&&game.Simulation!=null&&
            (game.State.Phase==RunPhase.Playing||game.State.Phase==RunPhase.LevelUp||game.State.Phase==RunPhase.Paused);
        public Rect VisibleLogicalRect
        {
            get
            {
                if(worldCamera==null)return default;
                float halfHeight=worldCamera.orthographicSize,halfWidth=halfHeight*worldCamera.aspect;
                var center=worldCamera.transform.position;
                var a=WorldProjection.SceneToWorld(center+new Vector3(-halfWidth,halfHeight));
                var b=WorldProjection.SceneToWorld(center+new Vector3(halfWidth,-halfHeight));
                return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
            }
        }
        Vector2 LogicalSize=>game!=null&&game.Content!=null
            ?new Vector2(game.Content.Config.ArenaWidth,game.Content.Config.ArenaHeight)
            :game!=null&&game.ArenaMap!=null?game.ArenaMap.LogicalSize:new Vector2(1440,1440);

        public void EditorSet(SurvivorGame session,Camera camera)
        {
            game=session;worldCamera=camera;generation=game!=null?game.Generation:-1;IsFollowing=false;
            if(worldCamera!=null){WorldProjection.ConfigureViewport(worldCamera);SetZoom(DefaultOrthographicSize);CenterOnPlayer();}
        }
        void Awake()
        {
            if(worldCamera==null)worldCamera=GetComponent<Camera>();
        }
        void Update()
        {
            RefreshView();
            if(!Application.isFocused||!InputAllowed||worldCamera==null)return;
            var mouse=Mouse.current;var keyboard=Keyboard.current;
            bool blocked=mouse!=null&&PointerOverUi(mouse.position.ReadValue());
            if(keyboard!=null&&keyboard.spaceKey.wasPressedThisFrame){FocusPlayer();return;}
            if(mouse==null||blocked)return;
            var screen=mouse.position.ReadValue();var viewport=worldCamera.pixelRect;
            float wheel=mouse.scroll.ReadValue().y;
            if(viewport.Contains(screen)&&Mathf.Abs(wheel)>.001f)
                SetZoom(worldCamera.orthographicSize*Mathf.Exp(-wheel/120f*WheelZoomRate));
            Vector2 direction=EdgePanDirection(screen,Screen.width,Screen.height);
            if(direction.sqrMagnitude>0)PanScene(direction.normalized*PanUnitsPerSecond*(worldCamera.orthographicSize/Mathf.Max(.001f,DefaultOrthographicSize))*Time.unscaledDeltaTime);
        }
        /// <summary>Both the field border and outer letterboxed frame can initiate a pan. UI gating is separate.</summary>
        public Vector2 EdgePanDirection(Vector2 screen,float screenWidth,float screenHeight)
        {
            var frame=WorldProjection.ViewportPixels(screenWidth,screenHeight);
            var field=WorldProjection.FieldViewportPixels(screenWidth,screenHeight);
            float band=EdgeBandPixels*frame.width/WorldProjection.ViewWidth;
            Vector2 direction=Vector2.zero;
            void AtBorder(Rect rect)
            {
                if(!rect.Contains(screen))return;
                if(screen.x<rect.xMin+band)direction.x=-1;else if(screen.x>rect.xMax-band)direction.x=1;
                if(screen.y<rect.yMin+band)direction.y=-1;else if(screen.y>rect.yMax-band)direction.y=1;
            }
            AtBorder(frame);AtBorder(field);
            return direction;
        }
        /// <summary>Refresh after simulation or viewport resize; also callable by editor tests.</summary>
        public void RefreshView()
        {
            if(worldCamera==null)return;
            WorldProjection.ConfigureViewport(worldCamera);
            if(game!=null&&generation!=game.Generation)
            {
                generation=game.Generation;IsFollowing=false;
                worldCamera.orthographicSize=DefaultOrthographicSize;CenterOnPlayer();
            }
            if(IsFollowing)CenterOnPlayer();
            ClampView();
        }
        public void FocusPlayer(){IsFollowing=true;CenterOnPlayer();ClampView();}
        public void PanToLogical(Vector2 position)
        {
            if(!Finite(position.x)||!Finite(position.y))return;
            IsFollowing=false;SetCenter(WorldProjection.WorldToScene(position));ClampView();
        }
        /// <summary>Screen-aligned scene displacement, useful for edge-pan and accessible controls.</summary>
        public void PanScene(Vector2 displacement)
        {
            if(worldCamera==null||!Finite(displacement.x)||!Finite(displacement.y))return;
            IsFollowing=false;SetCenter(worldCamera.transform.position+(Vector3)displacement);ClampView();
        }
        public void SetZoom(float orthographicSize)
        {
            if(worldCamera==null||!Finite(orthographicSize))return;
            worldCamera.orthographicSize=orthographicSize;ClampView();
        }
        void CenterOnPlayer()
        {
            Vector2 point=game!=null&&game.Simulation!=null?game.State.Player.Position:LogicalSize*.5f;
            SetCenter(WorldProjection.WorldToScene(point));
        }
        void SetCenter(Vector3 center)
        {
            if(worldCamera==null)return;
            center.z=-10;worldCamera.transform.SetPositionAndRotation(center,Quaternion.identity);
        }
        void ClampView()
        {
            if(worldCamera==null)return;
            Vector2 size=LogicalSize;var topLeft=WorldProjection.WorldToScene(Vector2.zero);var bottomRight=WorldProjection.WorldToScene(size);
            float aspect=WorldProjection.CameraAspect;
            worldCamera.aspect=aspect;
            float fit=Mathf.Max(.001f,Mathf.Min((topLeft.y-bottomRight.y)*.5f,(bottomRight.x-topLeft.x)/(2*aspect)));
            float maximum=Mathf.Min(Mathf.Max(.001f,MaximumOrthographicSize),fit);
            float minimum=Mathf.Min(Mathf.Max(.001f,MinimumOrthographicSize),maximum);
            worldCamera.orthographicSize=Mathf.Clamp(worldCamera.orthographicSize,minimum,maximum);
            float halfHeight=worldCamera.orthographicSize,halfWidth=halfHeight*aspect;
            var center=worldCamera.transform.position;
            center.x=Mathf.Clamp(center.x,topLeft.x+halfWidth,bottomRight.x-halfWidth);
            center.y=Mathf.Clamp(center.y,bottomRight.y+halfHeight,topLeft.y-halfHeight);
            SetCenter(center);
        }
        bool PointerOverUi(Vector2 screen)
        {
            var system=EventSystem.current;if(system==null)return false;
            if(pointer==null||pointerSystem!=system){pointerSystem=system;pointer=new PointerEventData(system);}
            pointer.Reset();pointer.position=screen;hits.Clear();system.RaycastAll(pointer,hits);
            return hits.Count>0;
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}

using SurvivalLegend.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalLegend.UI
{
    /// <summary>Overlay map of the whole authored arena; clicking it pans the world camera.</summary>
    [DefaultExecutionOrder(260)]
    public sealed class ArenaMinimapUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] SurvivorGame game;
        [SerializeField] ArenaMapView map;
        [SerializeField] FogOfWarPresenter fog;
        [SerializeField] ArenaCameraController cameraController;
        [SerializeField] RawImage terrain;
        [SerializeField] RawImage fogImage;
        [SerializeField] ArenaMinimapGraphic mapMarkers;
        [SerializeField] ArenaMinimapGraphic overlayMarkers;
        [SerializeField] RectTransform mapRect;
        EnvironmentDecoration[] obstacles;
        ArenaMapView cachedMap;
        Texture cachedTerrain;

        public Vector2 LogicalSize => map != null ? map.LogicalSize :
            game != null && game.Content != null
                ? new Vector2(game.Content.Config.ArenaWidth, game.Content.Config.ArenaHeight)
                : new Vector2(3600, 3600);

#if UNITY_EDITOR
        public void EditorSet(SurvivorGame runtime, ArenaMapView arena, FogOfWarPresenter fogPresenter,
            ArenaCameraController controller, RawImage floor, RawImage fogOverlay,
            ArenaMinimapGraphic world, ArenaMinimapGraphic overlay, RectTransform mapBounds)
        {
            game=runtime;map=arena;fog=fogPresenter;cameraController=controller;
            terrain=floor;fogImage=fogOverlay;mapMarkers=world;overlayMarkers=overlay;mapRect=mapBounds;
            RefreshReferences();
        }
#endif

        void OnEnable() => RefreshReferences();
        void LateUpdate()
        {
            if(game==null)game=SurvivorGame.Instance;
            if(game!=null && game.ArenaMap!=null && map!=game.ArenaMap)map=game.ArenaMap;
            if(map!=cachedMap)RefreshReferences();
            if(fogImage!=null)
            {
                var texture=fog!=null?fog.FogTexture:null;
                if(fogImage.texture!=texture)fogImage.texture=texture;
                fogImage.enabled=texture!=null;
            }
            if(mapMarkers!=null)mapMarkers.Refresh();
            if(overlayMarkers!=null)overlayMarkers.Refresh();
        }

        void RefreshReferences()
        {
            cachedMap=map;
            obstacles=map!=null?map.GetComponentsInChildren<EnvironmentDecoration>(true):new EnvironmentDecoration[0];
            Texture2D floor=map!=null && map.transform.Find("Backdrop")!=null
                ?map.transform.Find("Backdrop").GetComponent<SpriteRenderer>()?.sprite?.texture:null;
            if(terrain!=null && cachedTerrain!=floor)
            {
                cachedTerrain=floor;
                terrain.texture=floor;
                var size=LogicalSize;
                float tilesY=size.y/1440f;
                terrain.uvRect=new Rect(0,1f-tilesY,size.x/1440f,tilesY);
            }
            if(mapMarkers!=null)mapMarkers.Bind(this,false);
            if(overlayMarkers!=null)overlayMarkers.Bind(this,true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button!=PointerEventData.InputButton.Left || cameraController==null ||
                !cameraController.InputAllowed || mapRect==null)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect,eventData.position,
                eventData.pressEventCamera,out var local))return;
            Rect r=mapRect.rect;
            if(!r.Contains(local))return;
            var size=LogicalSize;
            float u=Mathf.Clamp01((local.x-r.xMin)/r.width);
            float v=Mathf.Clamp01((r.yMax-local.y)/r.height);
            cameraController.PanToLogical(new Vector2(u*size.x,v*size.y));
        }

        internal SurvivorGame Game=>game;
        internal FogOfWarPresenter Fog=>fog;
        internal ArenaCameraController CameraController=>cameraController;
        internal EnvironmentDecoration[] Obstacles=>obstacles;
    }

}

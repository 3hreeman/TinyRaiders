using SurvivalLegend.UI;
using SurvivalLegend.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalLegend.Editor
{
    /// <summary>Saved HUD hierarchy for the full-area interactive map.</summary>
    public static class MinimapAuthoring
    {
        static readonly Color Brass=new Color(.68f,.57f,.36f);
        static readonly Color Dark=new Color(.025f,.055f,.061f,.97f);

        public static ArenaMinimapUI Apply(SurvivorCanvasUI ui,SurvivorGame game,ArenaMapView map,
            FogOfWarPresenter fog,ArenaCameraController cameraController)
        {
            if(ui==null||ui.HudPanel==null)return null;
            var parent=ui.HudPanel.transform;
            var old=parent.Find("ArenaMinimap");
            if(old!=null)
            {
                var saved=old.GetComponent<ArenaMinimapUI>();
                if(saved!=null)
                {
                    var inner=old.Find("Inset/MapWell") as RectTransform;
                    saved.EditorSet(game,map,fog,cameraController,
                        inner!=null?inner.Find("Terrain")?.GetComponent<RawImage>():null,
                        inner!=null?inner.Find("Fog")?.GetComponent<RawImage>():null,
                        inner!=null?inner.Find("WorldMarkers")?.GetComponent<ArenaMinimapGraphic>():null,
                        inner!=null?inner.Find("OverlayMarkers")?.GetComponent<ArenaMinimapGraphic>():null,
                        inner);
                    EditorUtility.SetDirty(saved);
                    return saved;
                }
            }
            var root=Panel("ArenaMinimap",parent,1082,542,184,164,Brass,true);
            var uiMap=root.gameObject.AddComponent<ArenaMinimapUI>();
            var inset=Panel("Inset",root,2,2,180,160,Dark,false);
            Label("Heading",inset,"전장 지도",8,3,164,18,11,new Color(.82f,.79f,.61f),TextAnchor.MiddleLeft);
            var well=Panel("MapWell",inset,8,25,164,121,new Color(.035f,.075f,.076f),false);
            well.gameObject.AddComponent<RectMask2D>();
            var terrain=Raw("Terrain",well,0,0,164,121,new Color(.68f,.73f,.69f,.92f));
            var world=Rect("WorldMarkers",well,0,0,164,121).gameObject.AddComponent<ArenaMinimapGraphic>();
            world.raycastTarget=false;
            var fogImage=Raw("Fog",well,0,0,164,121,Color.white);
            fogImage.raycastTarget=false;
            var overlay=Rect("OverlayMarkers",well,0,0,164,121).gameObject.AddComponent<ArenaMinimapGraphic>();
            overlay.raycastTarget=false;
            Label("Instruction",inset,"클릭: 화면 이동  ·  SPACE 복귀",8,148,164,10,8,new Color(.60f,.67f,.64f),TextAnchor.MiddleCenter);
            uiMap.EditorSet(game,map,fog,cameraController,terrain,fogImage,world,overlay,well);
            EditorUtility.SetDirty(uiMap);
            return uiMap;
        }

        static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=new Vector2(0,1);
            rt.pivot=new Vector2(.5f,.5f);
            rt.sizeDelta=new Vector2(width,height);
            rt.anchoredPosition=new Vector2(x+width*.5f,-y-height*.5f);
            return rt;
        }
        static RectTransform Panel(string name,Transform parent,float x,float y,float width,float height,
            Color color,bool hit)
        {
            var rt=Rect(name,parent,x,y,width,height);
            var image=rt.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=hit;
            return rt;
        }
        static RawImage Raw(string name,Transform parent,float x,float y,float width,float height,Color color)
        {
            var raw=Rect(name,parent,x,y,width,height).gameObject.AddComponent<RawImage>();
            raw.color=color;raw.raycastTarget=false;return raw;
        }
        static Text Label(string name,Transform parent,string value,float x,float y,float width,float height,
            int size,Color color,TextAnchor align)
        {
            var label=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Text>();
            label.text=value;label.font=AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/SurvivalLegend/Resources/Art/Fonts/NotoSansKR-Regular.otf");
            label.fontSize=size;label.color=color;label.alignment=align;label.raycastTarget=false;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=Mathf.Max(7,size-2);
            label.resizeTextMaxSize=size;return label;
        }
    }
}

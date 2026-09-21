using System.IO;
using SurvivalLegend.World;
using SurvivalLegend.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalLegend.Editor.World
{
    public static class WorldExplorationBuilder
    {
        public static void Configure(WorldPresenter world,SurvivorGame game,Camera camera)
        {
            if(world==null||game==null||camera==null)return;
            var controller=camera.GetComponent<ArenaCameraController>();
            if(controller==null)controller=camera.gameObject.AddComponent<ArenaCameraController>();
            controller.EditorSet(game,camera);
            var fog=world.GetComponent<FogOfWarPresenter>();
            if(fog==null)fog=world.gameObject.AddComponent<FogOfWarPresenter>();
            var overlay=world.transform.Find("FogOfWar");
            if(overlay==null){overlay=new GameObject("FogOfWar",typeof(SpriteRenderer)).transform;overlay.SetParent(world.transform,false);}
            var renderer=overlay.GetComponent<SpriteRenderer>();
            const string folder="Assets/SurvivalLegend/Resources/Art/World";
            Directory.CreateDirectory(folder);string path=folder+"/FogOfWar.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                var shader=Shader.Find("SurvivalLegend/FXAlpha");
                if(shader==null)throw new System.InvalidOperationException("Missing fog alpha shader.");
                material=new Material(shader);AssetDatabase.CreateAsset(material,path);
            }
            renderer.sharedMaterial=material;renderer.sortingOrder=30000;renderer.color=Color.white;
            fog.EditorSet(game,renderer);world.EditorSetFog(fog);
            var router=UnityEngine.Object.FindAnyObjectByType<PlayerInputRouter>();
            if(router!=null){router.Fog=fog;EditorUtility.SetDirty(router);}
            var ui=UnityEngine.Object.FindAnyObjectByType<SurvivorCanvasUI>();
            if(ui!=null)
            {
                SurvivalLegend.Editor.MinimapAuthoring.Apply(ui,game,world.Arena,fog,controller);
                foreach(var text in ui.GetComponentsInChildren<Text>(true))
                    if(text.name=="PauseHelp")text.text="ESC  계속하기 · SPACE  플레이어 추적";
                EditorUtility.SetDirty(ui);
            }
            EditorUtility.SetDirty(camera);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(fog);EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();
        }
    }
}

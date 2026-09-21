using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SurvivalLegend.Editor.Data;
using SurvivalLegend.Editor.World;
using SurvivalLegend.World;

namespace SurvivalLegend.Editor
{
    /// <summary>Explicit, idempotent authoring commands. Nothing runs during Play or on editor reload.</summary>
    public static class MigrationBootstrap
    {
        public const string ScenePath="Assets/SurvivalLegend/Scenes/SurvivalLegendGameObjects.unity";

        [MenuItem("Survival Legend/Migration/Ensure Authored Assets")]
        public static void EnsureProject()
        {
            GameDatabaseSeed.EnsureDatabase();
            WorldPrefabBuilder.EnsureAssets();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Survival Legend/Migration/Create or Open GameObject Scene")]
        public static void CreateScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EnsureProject();
            // An existing authored scene belongs to the designer: never regenerate over its edits.
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);return;}
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var systems=new GameObject("Systems");
            var session=new GameObject("Game Session");session.transform.SetParent(systems.transform,false);
            var game=session.AddComponent<SurvivorGame>();game.Database=GameDatabaseSeed.EnsureDatabase();
            var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.024f,.035f,.052f);WorldProjection.ConfigureCamera(camera);
            cameraObject.AddComponent<AudioListener>();
            var inputObject=new GameObject("Player Input Router");inputObject.transform.SetParent(systems.transform,false);
            var input=inputObject.AddComponent<PlayerInputRouter>();input.Game=game;input.WorldCamera=camera;
            var world=new GameObject("World");WorldPrefabBuilder.ConfigureScene(world,game,camera);
            game.ArenaMap=world.GetComponentInChildren<ArenaMapView>(true);
            var ui=new GameObject("UI");CanvasUiBuilder.Build(game,ui.transform);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Selection.activeGameObject=session;
        }

        [MenuItem("Survival Legend/Migration/Use Verified Scene as Default")]
        public static void SetAsDefaultScene()
        {
            if(!File.Exists(ScenePath))throw new InvalidOperationException("Create and verify the GameObject scene first.");
            var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(ScenePath,true)};
            foreach(var scene in EditorBuildSettings.scenes)if(scene.path!=ScenePath)scenes.Add(new EditorBuildSettingsScene(scene.path,false));
            EditorBuildSettings.scenes=scenes.ToArray();
            PlayerSettings.productName="Survival Legend";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivalLegend.Editor
{
    public static class SurvivalLegendBootstrap
    {
        public const string ScenePath="Assets/SurvivalLegend/Scenes/SurvivalLegend.unity";
        [MenuItem("Survival Legend/Legacy/Create First Playable Scene")]
        public static void CreateScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.024f,.035f,.052f);camera.transform.position=new Vector3(0,0,-10);
            cameraObject.AddComponent<AudioListener>();
            var root=new GameObject("Survival Legend");root.AddComponent<SurvivorGame>();
            // Presentation lives in the runtime assembly; discovery keeps the editor setup independent of its filename.
            foreach(var type in typeof(SurvivorGame).Assembly.GetTypes()) if(type.Namespace=="SurvivalLegend.Presentation"&&typeof(MonoBehaviour).IsAssignableFrom(type)&&!type.IsAbstract&&type.Name=="SurvivorPresentation") root.AddComponent(type);
            Directory.CreateDirectory("Assets/SurvivalLegend/Scenes");
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.Refresh();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.productName="Survival Legend";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            Selection.activeGameObject=root;
        }
        [MenuItem("Survival Legend/Build Windows")]
        public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Windows/SurvivalLegend.exe");
        [MenuItem("Survival Legend/Build WebGL")]
        public static void BuildWebGL()=>Build(BuildTarget.WebGL,"Builds/WebGL");
        static void Build(BuildTarget target,string path)
        {
            var scenes=EditorBuildSettings.scenes.Where(scene=>scene.enabled).Select(scene=>scene.path).ToArray();
            if(scenes.Length==0||scenes.Any(scene=>!File.Exists(scene)))throw new InvalidOperationException("Select an existing enabled scene in Build Settings before building.");
            if(!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target),target))throw new InvalidOperationException("Install the Unity build support module for "+target+" in Unity Hub.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=path,target=target,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+report.summary.result);
        }
    }
}

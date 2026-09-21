using System;
using SurvivalLegend.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivalLegend.Editor.World
{
    /// <summary>Swaps the authored map while preserving the session, HUD and actor bindings.</summary>
    public static class EnvironmentSceneBuilder
    {
        [MenuItem("Survival Legend/Environments/Apply Grassland")]
        public static void ApplyGrassland()=>Apply("grassland");
        [MenuItem("Survival Legend/Environments/Apply Desert")]
        public static void ApplyDesert()=>Apply("desert");
        [MenuItem("Survival Legend/Environments/Apply Cave")]
        public static void ApplyCave()=>Apply("cave");

        public static void Apply(string id)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before replacing the authored environment.");
            var game=UnityEngine.Object.FindAnyObjectByType<SurvivorGame>();
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPresenter>();
            if(game==null||world==null)throw new InvalidOperationException("Open SurvivalLegendGameObjects before applying an environment.");
            EnvironmentArtBuilder.EnsureAssets();
            var prefab=EnvironmentArtBuilder.LoadMapPrefab(id);
            if(prefab==null)throw new InvalidOperationException("Unknown environment: "+id);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,world.transform);
            Undo.RegisterCreatedObjectUndo(instance,"Change environment");
            instance.transform.localPosition=Vector3.zero;
            var next=instance.GetComponent<ArenaMapView>();
            // Validate before removing the current map; failed authoring leaves it intact.
            try { next.BuildSnapshot(game.Database.BuildSnapshot()); }
            catch { UnityEngine.Object.DestroyImmediate(instance);throw; }
            if(world.Arena!=null)Undo.DestroyObjectImmediate(world.Arena.gameObject);
            game.ArenaMap=next;
            WorldPrefabBuilder.ConfigureScene(world.gameObject,game,Camera.main);
            PrefabUtility.RecordPrefabInstancePropertyModifications(game);
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            EditorSceneManager.SaveScene(world.gameObject.scene);
            Selection.activeGameObject=instance;
        }
    }
}

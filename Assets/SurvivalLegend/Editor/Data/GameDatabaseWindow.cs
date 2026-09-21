#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SurvivalLegend.Data;

namespace SurvivalLegend.Editor.Data
{
    public sealed class GameDatabaseWindow : EditorWindow
    {
        GameDatabase database; string search = string.Empty; Vector2 scroll;
        [MenuItem("Survival Legend/Data/Database Manager")]
        static void Open() { GetWindow<GameDatabaseWindow>("SL Data Manager"); }
        void OnGUI()
        {
            EditorGUILayout.LabelField("Survival Legend Content", EditorStyles.boldLabel);
            database=(GameDatabase)EditorGUILayout.ObjectField("Database",database,typeof(GameDatabase),false);
            if(database==null) { if(GUILayout.Button("Create / Load Default Database"))database=GameDatabaseSeed.EnsureDatabase(); return; }
            search=EditorGUILayout.TextField("Search",search);
            using(new EditorGUILayout.HorizontalScope()) { if(GUILayout.Button("Validate")) ShowValidation(); if(GUILayout.Button("Save")){EditorUtility.SetDirty(database);AssetDatabase.SaveAssets();} if(GUILayout.Button("Select Database"))Selection.activeObject=database; }
            scroll=EditorGUILayout.BeginScrollView(scroll);
            DrawRegistry("Characters",database.Characters);DrawRegistry("Skills",database.Skills);DrawRegistry("Specials",database.Specials);DrawRegistry("Enemies",database.Enemies);DrawRegistry("Normal Augments",database.NormalAugments);DrawRegistry("Special Augments",database.SpecialAugments);
            EditorGUILayout.EndScrollView();
        }
        void DrawRegistry<T>(string title,List<T> list) where T:ContentDefinition
        {
            EditorGUILayout.Space(8);using(new EditorGUILayout.HorizontalScope()){EditorGUILayout.LabelField(title+" ("+list.Count+")",EditorStyles.boldLabel);if(GUILayout.Button("Create",GUILayout.Width(70))){Undo.RecordObject(database,"Add "+title);list.Add(Create<T>(list));EditorUtility.SetDirty(database);}}
            for(var i=list.Count-1;i>=0;i--){var item=list[i];if(!Matches(item))continue;using(new EditorGUILayout.HorizontalScope()){if(item==null)EditorGUILayout.LabelField("Missing reference",EditorStyles.miniBoldLabel);else EditorGUILayout.ObjectField(item,typeof(T),false);if(item!=null&&GUILayout.Button("Duplicate",GUILayout.Width(72))){Undo.RecordObject(database,"Duplicate "+title);list.Add(Duplicate(item,list));EditorUtility.SetDirty(database);}if(GUILayout.Button("Remove",GUILayout.Width(62))){Undo.RecordObject(database,"Remove "+title);list.RemoveAt(i);EditorUtility.SetDirty(database);}}}
        }
        bool Matches(ContentDefinition item){if(item==null)return string.IsNullOrEmpty(search)||"missing reference".IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0;return string.IsNullOrEmpty(search)||(item.name??string.Empty).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||(item.Id??string.Empty).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0;}
        T Create<T>(IList<T> registry) where T:ContentDefinition { var folder=GameDatabaseSeed.Root+"/User";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(GameDatabaseSeed.Root,"User");var item=ScriptableObject.CreateInstance<T>();item.name="new-"+typeof(T).Name;item.Id=UniqueId(registry,item.name);AssetDatabase.CreateAsset(item,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+item.name+".asset"));Undo.RegisterCreatedObjectUndo(item,"Create "+typeof(T).Name);return item; }
        T Duplicate<T>(T original,IList<T> registry) where T:ContentDefinition { var clone=Instantiate(original);clone.name=original.name+" Copy";clone.Id=UniqueId(registry,(original.Id??original.name)+"-copy");var path=AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(original).Replace(".asset"," Copy.asset"));AssetDatabase.CreateAsset(clone,path);Undo.RegisterCreatedObjectUndo(clone,"Duplicate "+typeof(T).Name);return clone; }
        static string UniqueId<T>(IList<T> registry,string baseId) where T:ContentDefinition {var used=new HashSet<string>(StringComparer.Ordinal);foreach(var item in registry)if(item!=null&&!string.IsNullOrEmpty(item.Id))used.Add(item.Id);var result=string.IsNullOrEmpty(baseId)?"new-content":baseId;var number=2;while(used.Contains(result))result=baseId+"-"+number++;return result;}
        void ShowValidation(){var errors=database.Validate();EditorUtility.DisplayDialog("Game Database Validation",errors.Count==0?"Database is valid.":string.Join("\n",errors),"OK");}
    }
}
#endif

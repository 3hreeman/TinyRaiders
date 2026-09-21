using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SurvivalLegend.Data;
using SurvivalLegend.UI;
using SurvivalLegend.World;

namespace SurvivalLegend.Tests
{
    public class AuthoredAssetsTests
    {
        [Test]
        public void ScriptableObjectRegistryEditsAreValidatedAndSnapshotsAreIndependent()
        {
            var original = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Resources/Data/SurvivalLegendGameDatabase.asset");
            var copy = Object.Instantiate(original);
            var skill = Object.Instantiate(original.Skills[0]);
            try
            {
                var before = copy.BuildSnapshot();
                skill.Id = "test-added-skill"; copy.Skills.Add(skill);
                var added = copy.BuildSnapshot();
                Assert.That(added.Skills.ContainsKey(skill.Id), Is.True);
                Assert.That(before.Skills.ContainsKey(skill.Id), Is.False);
                float originalCooldown = skill.Cooldown;
                skill.Cooldown += 7;
                Assert.That(added.Skills[skill.Id].Cooldown, Is.EqualTo(originalCooldown));
                Assert.That(copy.BuildSnapshot().Skills[skill.Id].Cooldown, Is.EqualTo(originalCooldown + 7));
                copy.Skills.Remove(skill);
                Assert.That(copy.BuildSnapshot().Skills.ContainsKey(skill.Id), Is.False);
                skill.Id = copy.Skills[0].Id; copy.Skills.Add(skill);
                Assert.That(copy.Validate().Exists(message => message.Contains("Duplicate")), Is.True);
                copy.Skills.Remove(skill); copy.Skills.Remove(copy.Characters[0].Skills[0]);
                Assert.That(copy.Validate().Exists(message => message.Contains("unregistered skill")), Is.True);
                Assert.That(original.Validate(), Is.Empty, "Editing the copy must not change authored assets.");
            }
            finally { Object.DestroyImmediate(skill); Object.DestroyImmediate(copy); }
        }

        [Test]
        public void DatabaseDefinitionsHaveReloadableMonoScriptsAndValidReferences()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Resources/Data/SurvivalLegendGameDatabase.asset");
            Assert.That(database, Is.Not.Null, "Generate the authored database before validating the project.");
            Assert.That(database.Validate(), Is.Empty);
            var definitions = AssetDatabase.FindAssets("t:ContentDefinition", new[] { "Assets/Resources/Data" });
            Assert.That(definitions, Is.Not.Empty);
            foreach (var guid in definitions)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var script = MonoScript.FromScriptableObject(asset);
                Assert.That(script, Is.Not.Null, asset.name);
                Assert.That(script.GetClass(), Is.EqualTo(asset.GetType()), asset.name + " needs a matching script filename.");
            }
            var first = database.BuildSnapshot(); var second = database.BuildSnapshot();
            Assert.That(first, Is.Not.SameAs(second));
            foreach (var id in first.Characters.Keys) Assert.That(first.Characters[id].Skills, Is.Not.SameAs(second.Characters[id].Skills));
        }

        [Test]
        public void ReusableCanvasPrefabsKeepAllAuthoringReferences()
        {
            const string root = "Assets/SurvivalLegend/Prefabs/UI/";
            var character = AssetDatabase.LoadAssetAtPath<GameObject>(root + "CharacterCard.prefab").GetComponent<CharacterCardUI>();
            Assert.That(character.Select, Is.Not.Null); Assert.That(character.Portrait, Is.Not.Null); Assert.That(character.Selection, Is.Not.Null);
            var augment = AssetDatabase.LoadAssetAtPath<GameObject>(root + "AugmentCard.prefab").GetComponent<AugmentCardUI>();
            Assert.That(augment.Choose, Is.Not.Null); Assert.That(augment.Reroll, Is.Not.Null); Assert.That(augment.Description, Is.Not.Null);
            foreach (var graphic in augment.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name + " must render safely inside the card viewport mask.");
            var slot = AssetDatabase.LoadAssetAtPath<GameObject>(root + "SkillSlot.prefab").GetComponent<SkillSlotUI>();
            Assert.That(slot.Cast, Is.Not.Null); Assert.That(slot.Icon, Is.Not.Null); Assert.That(slot.QuickCast, Is.Not.Null);
            Assert.That(slot.CooldownFill.sprite, Is.Not.Null, "Filled images need a sprite to visualize cooldown.");
        }

        [Test]
        public void SavedWorldVisualDatabaseHasFallbacksAndEightDirections()
        {
            var guids = AssetDatabase.FindAssets("t:WorldVisualDatabase"); Assert.That(guids, Is.Not.Empty);
            var database = AssetDatabase.LoadAssetAtPath<WorldVisualDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var actor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalLegend/Prefabs/World/ActorBase.prefab").GetComponent<ActorView>();
            var serialized = new SerializedObject(actor);
            Assert.That(serialized.FindProperty("windupLine").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("empowermentRing").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("overloadOrbs").arraySize, Is.EqualTo(3));
            Assert.That(database.Character("added-character"), Is.Not.Null);
            Assert.That(database.Enemy("added-enemy"), Is.Not.Null);
            foreach (string id in new[] { "swordsman", "archer", "mage" })
            {
                var entry = database.Character(id); Assert.That(entry.prefab, Is.Not.Null);
                for (int direction = 0; direction < 8; direction++)
                    Assert.That(entry.spriteSet.Get(direction * Mathf.PI / 4, false, false, 0, 0), Is.Not.Null);
            }
        }

        [Test]
        public void SavedSceneKeepsMinimapExplorationAndCameraReferencesAfterReapply()
        {
            var scene=EditorSceneManager.OpenScene("Assets/SurvivalLegend/Scenes/SurvivalLegendGameObjects.unity",OpenSceneMode.Additive);
            try
            {
                var minimaps=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<ArenaMinimapUI>(true)).ToArray();
                Assert.That(minimaps.Length,Is.EqualTo(1));
                var serialized=new SerializedObject(minimaps[0]);
                foreach(var field in new[]{"game","map","fog","cameraController","terrain","fogImage","mapMarkers","overlayMarkers","mapRect"})
                    Assert.That(serialized.FindProperty(field).objectReferenceValue,Is.Not.Null,"Minimap is missing saved reference: "+field);
                var map=serialized.FindProperty("map").objectReferenceValue as ArenaMapView;
                Assert.That(map.LogicalSize,Is.EqualTo(new Vector2(3600,3600)));
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
    }
}

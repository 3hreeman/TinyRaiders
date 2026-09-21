using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using SurvivalLegend.Data;
using SurvivalLegend.World;

namespace SurvivalLegend.Tests
{
    /// <summary>FX IDs are data-driven; adding an authored kit must not silently omit its visual profile.</summary>
    public class FxCatalogCoverageTests
    {
        const string CatalogPath = "Assets/SurvivalLegend/Resources/FX/SkillFxCatalog.asset";

        [Test]
        public void AuthoredRosterSpecialsZonesAndTransitionEffectsHaveUsableProfiles()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkillFxCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, "Run the FX authoring migration before validating FX coverage.");
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Resources/Data/SurvivalLegendGameDatabase.asset");
            Assert.That(database, Is.Not.Null);
            var content = database.BuildSnapshot();
            var ids = new HashSet<string>();
            foreach (var character in content.Characters.Values)
                foreach (var skill in character.Skills) ids.Add(skill);
            foreach (var special in content.Specials.Keys) ids.Add(special);
            ids.UnionWith(new[]
            {
                "event.levelup", "event.ring", "event.slash", "event.lightning", "event.dodge", "event.move",
                "zone.rain", "zone.spin", "zone.sword", "zone.sword.impact", "zone.blackhole", "zone.hazard"
            });

            foreach (var id in ids)
            {
                var profile = catalog.Find(id);
                Assert.That(profile, Is.Not.Null, "Missing FX profile: " + id);
                Assert.That(profile.Prefab, Is.Not.Null, "FX profile needs a pooled prefab: " + id);
                Assert.That(profile.Duration, Is.GreaterThan(0));
                Assert.That(profile.Tail, Is.GreaterThan(0));
            }
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SurvivalLegend.Tests
{
    /// <summary>Presentation events identify successful special casts without participating in simulation state/RNG.</summary>
    public class SpecialFxEventTests
    {
        [TestCase("heal")]
        [TestCase("haste")]
        [TestCase("barrier")]
        [TestCase("blackhole")]
        public void SuccessfulSpecialEmitsItsOwnIdentifiableVisualEvent(string id)
        {
            var other = id == "heal" ? "haste" : "heal";
            var game = new GameSimulation(741, id, other);
            if (id == "heal") game.State.Player.Hp = game.State.Player.MaxHp * .5f;
            uint seed = game.State.Seed;
            Vector2 requested = game.State.Player.Position + new Vector2(1000, 0);

            Assert.That(game.UseSpecial(0, requested), Is.True);
            var events = new List<EffectState>();
            game.DrainVisualEvents(events);
            var definition = game.Content.Specials[id];
            var effect = events.Find(item => item.Kind == "special" && item.Text == id);

            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.Color, Is.EqualTo(definition.Color));
            Assert.That(effect.Life, Is.EqualTo(.8f).Within(.0001f));
            Assert.That(effect.Radius, Is.EqualTo(id == "blackhole" ? definition.Radius : 65).Within(.0001f));
            Assert.That(effect.Position, Is.EqualTo(id == "blackhole" ? game.State.Player.Position + Vector2.right * definition.Range : game.State.Player.Position));
            Assert.That(game.State.Seed, Is.EqualTo(seed), "Visual event emission must not consume gameplay RNG.");

            // The visual event must not consume the combat entity sequence. Blackhole itself owns zone id 1;
            // every other special leaves the first spawned enemy at id 1.
            if (id == "blackhole") Assert.That(game.State.Zones[0].Id, Is.EqualTo(1));
            else Assert.That(game.SpawnEnemy("chaser", new Vector2(100, 100)).Id, Is.EqualTo(1));
        }

        [Test]
        public void FailedHealAndCooldownDoNotEmitSpecialVisualEvents()
        {
            var heal = new GameSimulation(91, "heal", "haste");
            uint healSeed = heal.State.Seed;
            Assert.That(heal.UseSpecial(0, heal.State.Player.Position), Is.False, "Full-health heal must fail.");
            var events = new List<EffectState>();
            heal.DrainVisualEvents(events);
            Assert.That(events.Find(item => item.Kind == "special"), Is.Null);
            Assert.That(heal.State.Seed, Is.EqualTo(healSeed));
            Assert.That(heal.SpawnEnemy("chaser", new Vector2(100, 100)).Id, Is.EqualTo(1));

            var haste = new GameSimulation(92, "haste", "heal");
            Assert.That(haste.UseSpecial(0, haste.State.Player.Position), Is.True);
            haste.DrainVisualEvents(events); events.Clear();
            uint cooldownSeed = haste.State.Seed;
            Assert.That(haste.UseSpecial(0, haste.State.Player.Position), Is.False, "Cooldown special must fail.");
            haste.DrainVisualEvents(events);
            Assert.That(events.Find(item => item.Kind == "special"), Is.Null);
            Assert.That(haste.State.Seed, Is.EqualTo(cooldownSeed));
            Assert.That(haste.SpawnEnemy("chaser", new Vector2(100, 100)).Id, Is.EqualTo(1));
        }
    }
}

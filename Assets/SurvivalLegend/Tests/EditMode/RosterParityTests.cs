using NUnit.Framework;
using UnityEngine;

namespace SurvivalLegend.Tests
{
    public class RosterParityTests
    {
        static GameSimulation Run(string character)
        {
            var game = new GameSimulation(7, "heal", "haste", character);
            game.State.Player.Position = new Vector2(640, 360);
            game.State.Player.CritChance = 0;
            game.State.Player.Armor = 0;
            return game;
        }

        static EnemyState Dummy(GameSimulation game, float x, float y = 360)
        {
            var enemy = game.SpawnEnemy("chaser", new Vector2(x, y));
            enemy.Hp = enemy.MaxHp = 10000;
            enemy.Speed = 0;
            enemy.AttackTimer = 999;
            return enemy;
        }

        static void Advance(GameSimulation game, float seconds)
        {
            for (int i = 0; i < Mathf.RoundToInt(seconds * 100); i++) game.Step(.01f);
        }

        static void Grant(GameSimulation game, string id)
        {
            game.State.Phase = RunPhase.Augment;
            game.State.AugmentChoices.Clear();
            game.State.AugmentChoices.Add(new AugmentChoice { Id = id, Title = id, Tier = "special" });
            Assert.That(game.ChooseAugment(0), Is.True);
            game.State.Player.CritChance = 0;
            game.State.Player.Armor = 0;
        }

        [TestCase("swordsman", 180, 164, 25.6f, 1.3f, 112, 5, "sword-")]
        [TestCase("archer", 120, 216, 17.6f, 1.7f, 300, 3, "bow-")]
        [TestCase("mage", 90, 188, 22.4f, .72f, 330, 3, "magic-")]
        public void EachClassStartsWithOriginalStatsAndItsOwnFourSkills(string id, float hp,
            float speed, float damage, float attackSpeed, float range, float regen, string prefix)
        {
            var game = Run(id); var p = game.State.Player;
            Assert.That(game.State.Character, Is.EqualTo(id));
            Assert.That(p.Hp, Is.EqualTo(hp));
            Assert.That(p.Speed, Is.EqualTo(speed));
            Assert.That(p.Damage, Is.EqualTo(damage).Within(.001));
            Assert.That(p.AttackSpeed, Is.EqualTo(attackSpeed).Within(.001));
            Assert.That(p.Range, Is.EqualTo(range));
            Assert.That(p.HealthRegen, Is.EqualTo(regen));
            for (int i = 0; i < 4; i++)
                Assert.That(game.State.Skills[i].Id, Is.EqualTo(prefix + "qwer"[i]));
        }

        [Test] public void SwordEmpowerHitsPrimaryAndNearbyEnemiesOnlyOnce()
        {
            var game = Run("swordsman");
            var a = Dummy(game, 715); var b = Dummy(game, 775); var far = Dummy(game, 950);
            game.CastSkill(0, a.Position); game.Attack(a.Id); game.Step(.01f);
            Assert.That(a.Hp, Is.EqualTo(10000 - 25.6f * 2).Within(.002));
            Assert.That(b.Hp, Is.EqualTo(10000 - 25.6f * .75f).Within(.002));
            Assert.That(far.Hp, Is.EqualTo(10000));
            Advance(game, .78f);
            Assert.That(a.Hp, Is.EqualTo(10000 - 25.6f * 3).Within(.004));
            Assert.That(b.Hp, Is.EqualTo(10000 - 25.6f * .75f).Within(.002));
        }

        [Test] public void SwordShieldReflectsToSourceEvenWhenAbsorptionIsExhausted()
        {
            var game = Run("swordsman"); var a = Dummy(game, 800); var b = Dummy(game, 820);
            game.CastSkill(1, a.Position);
            game.DamagePlayer(20, a.Id);
            Assert.That(game.State.Player.Hp, Is.EqualTo(180));
            Assert.That(a.Hp, Is.EqualTo(9994).Within(.002));
            Assert.That(b.Hp, Is.EqualTo(10000));
            game.State.Player.Invulnerable = 0; game.DamagePlayer(30, a.Id);
            Assert.That(game.State.Player.Hp, Is.EqualTo(166).Within(.002));
            Assert.That(a.Hp, Is.EqualTo(9985).Within(.002));
            game.State.Player.Invulnerable = 0; game.DamagePlayer(10, a.Id);
            Assert.That(a.Hp, Is.EqualTo(9982).Within(.002));
            Advance(game, 3.1f); game.State.Player.Invulnerable = 0; game.DamagePlayer(10, a.Id);
            Assert.That(a.Hp, Is.EqualTo(9982).Within(.002));
        }

        [Test] public void SwordSpinDealsTwentyTicksThenExpires()
        {
            var game = Run("swordsman"); var a = Dummy(game, 700);
            game.CastSkill(2, a.Position); Advance(game, 5);
            Assert.That(a.Hp, Is.EqualTo(10000 - 25.6f * .75f * 20).Within(.03));
            Assert.That(game.State.Zones.Count, Is.EqualTo(0));
        }

        [Test] public void SwordSpinFollowsMovementAndAppliesItsSpeedBuff()
        {
            var game = Run("swordsman");
            game.CastSkill(2, game.State.Player.Position); game.MoveTo(new Vector2(1000, 360));
            Advance(game, .4f);
            Assert.That(game.State.Player.Position.x, Is.EqualTo(640 + 164 * 1.15f * .4f).Within(.02));
            Assert.That(Vector2.Distance(game.State.Zones[0].Position, game.State.Player.Position), Is.LessThan(.01f));
        }

        [Test] public void SwordUltimateWarnsThenDealsCoreAndOuterDamageWithoutChargingItself()
        {
            var game = Run("swordsman");
            var center = Dummy(game, 850); var outer = Dummy(game, 970); var far = Dummy(game, 1100);
            game.State.Ultimate = 100; game.CastSkill(3, center.Position); Advance(game, .3f);
            Assert.That(center.Hp, Is.EqualTo(10000));
            Advance(game, .3f);
            Assert.That(center.Hp, Is.EqualTo(10000 - 25.6f * 20).Within(.01));
            Assert.That(outer.Hp, Is.EqualTo(10000 - 25.6f * 10).Within(.01));
            Assert.That(far.Hp, Is.EqualTo(10000));
            Assert.That(game.State.Ultimate, Is.EqualTo(0));
        }

        [Test] public void MageChainHitsThreeUniqueTargetsWithDecreasingDamage()
        {
            var game = Run("mage");
            var a = Dummy(game, 800); var b = Dummy(game, 925); var c = Dummy(game, 1050); var far = Dummy(game, 1250);
            game.Attack(a.Id); Advance(game, .31f);
            Assert.That(a.Hp, Is.EqualTo(10000 - 22.4f).Within(.002));
            Assert.That(b.Hp, Is.EqualTo(10000 - 22.4f * .75f).Within(.002));
            Assert.That(c.Hp, Is.EqualTo(10000 - 22.4f * .5f).Within(.002));
            Assert.That(far.Hp, Is.EqualTo(10000));
            Assert.That(game.State.Ultimate, Is.EqualTo(15));
        }

        [Test] public void MageFireballExplodesOncePerNearbyEnemy()
        {
            var game = Run("mage");
            var a = Dummy(game, 800); var b = Dummy(game, 800, 420); var far = Dummy(game, 1000);
            game.CastSkill(0, a.Position); Advance(game, .7f);
            Assert.That(a.Hp, Is.EqualTo(10000 - 22.4f * 1.5f).Within(.003));
            Assert.That(b.Hp, Is.EqualTo(10000 - 22.4f * 1.5f).Within(.003));
            Assert.That(far.Hp, Is.EqualTo(10000));
        }

        [Test] public void MageFrostDamagesAndRootsOnlyInsideItsArea()
        {
            var game = Run("mage"); var near = Dummy(game, 790); var far = Dummy(game, 1000);
            game.CastSkill(1, game.State.Player.Position);
            Assert.That(near.Hp, Is.EqualTo(10000 - 22.4f).Within(.002));
            Assert.That(near.Root, Is.EqualTo(1).Within(.001));
            Assert.That(far.Hp, Is.EqualTo(10000));
            Assert.That(far.Root, Is.EqualTo(0));
        }

        [Test] public void MageBlinkClampsDistanceStopsOrderAndDamagesAtDestination()
        {
            var game = Run("mage"); var near = Dummy(game, 920); var far = Dummy(game, 680);
            game.MoveTo(new Vector2(1200, 360)); game.CastSkill(2, new Vector2(1400, 360));
            Assert.That(game.State.Player.Position.x, Is.EqualTo(920).Within(.001));
            Assert.That(game.State.Destination, Is.Null);
            Assert.That(game.State.Order, Is.EqualTo("idle"));
            Assert.That(near.Hp, Is.EqualTo(10000 - 22.4f).Within(.002));
            Assert.That(far.Hp, Is.EqualTo(10000));
        }

        [Test] public void MageOverloadDoublesDamageAndAcceleratesOnlySkillRecovery()
        {
            var game = Run("mage"); var enemy = Dummy(game, 790);
            game.State.Ultimate = 100; game.CastSkill(3, enemy.Position);
            game.CastSkill(1, enemy.Position); game.UseSpecial(1, enemy.Position);
            Assert.That(enemy.Hp, Is.EqualTo(10000 - 22.4f * 2).Within(.003));
            Advance(game, .5f);
            Assert.That(game.State.Skills[1].Remaining, Is.EqualTo(7).Within(.005));
            Assert.That(game.State.Specials[1].Remaining, Is.EqualTo(59.5f).Within(.005));
            Advance(game, 9.6f);
            float hp = enemy.Hp; game.CastSkill(1, enemy.Position);
            Assert.That(enemy.Hp, Is.EqualTo(hp - 22.4f).Within(.003));
        }

        [TestCase("swordsman")] [TestCase("mage")]
        public void RestartCreatesCleanClassState(string character)
        {
            var first = Run(character); first.State.Ultimate = 100; first.CastSkill(3, new Vector2(850, 360));
            var retry = Run(character);
            Assert.That(retry.State.Character, Is.EqualTo(character));
            Assert.That(retry.State.Level, Is.EqualTo(1));
            Assert.That(retry.State.Ultimate, Is.EqualTo(0));
            Assert.That(retry.State.History, Is.Empty);
            Assert.That(retry.State.Player.Hp, Is.EqualTo(retry.State.Player.MaxHp));
        }

        [Test] public void SwordChargeAugmentEmpowersTwoHitsThenReturnsToNormal()
        {
            var game = Run("swordsman"); Grant(game, "sword-q-charges"); var enemy = Dummy(game, 715);
            game.CastSkill(0, enemy.Position); game.Attack(enemy.Id); Advance(game, 1.6f);
            Assert.That(enemy.Hp, Is.EqualTo(10000 - 25.6f * 5).Within(.01));
        }

        [Test] public void SwordGuardAugmentAddsTemporaryMovementSpeed()
        {
            var game = Run("swordsman"); Grant(game, "sword-w-speed");
            game.CastSkill(1, game.State.Player.Position); game.MoveTo(new Vector2(1300, 360)); Advance(game, .4f);
            Assert.That(game.State.Player.Position.x, Is.EqualTo(640 + 164 * 1.2f * .4f).Within(.02));
        }

        [Test] public void SwordUltimateAreaAugmentExpandsPreviewAndActualCore()
        {
            var game = Run("swordsman"); Grant(game, "sword-r-area");
            Assert.That(game.GetSkillTargeting(3).Radius, Is.EqualTo(270).Within(.001));
            Assert.That(game.GetSkillTargeting(3).CoreRadius, Is.EqualTo(90).Within(.001));
            var core = Dummy(game, 930); var outer = Dummy(game, 1090);
            game.State.Ultimate = 100; game.CastSkill(3, new Vector2(850, 360)); Advance(game, .6f);
            Assert.That(core.Hp, Is.EqualTo(10000 - 25.6f * 20).Within(.01));
            Assert.That(outer.Hp, Is.EqualTo(10000 - 25.6f * 10).Within(.01));
        }

        [Test] public void MageChainAugmentAddsFourthUniqueTarget()
        {
            var game = Run("mage"); Grant(game, "mage-basic-chain");
            var first = Dummy(game, 800); Dummy(game, 925); Dummy(game, 1050); var fourth = Dummy(game, 1175);
            game.Attack(first.Id); Advance(game, .5f);
            Assert.That(fourth.Hp, Is.EqualTo(10000 - 22.4f * .5f).Within(.003));
        }

        [Test] public void MageFireballPenetrationAugmentExplodesAtSecondTarget()
        {
            var game = Run("mage"); Grant(game, "magic-q-pierce");
            var first = Dummy(game, 800); var second = Dummy(game, 1020);
            game.CastSkill(0, second.Position); Advance(game, 1);
            Assert.That(first.Hp, Is.EqualTo(10000 - 22.4f * 1.5f).Within(.003));
            Assert.That(second.Hp, Is.EqualTo(10000 - 22.4f * 1.5f).Within(.003));
        }

        [Test] public void MageRangeAugmentChangesTeleportAndAimPreviewTogether()
        {
            var game = Run("mage"); Grant(game, "magic-e-range");
            Assert.That(game.GetSkillTargeting(2).Range, Is.EqualTo(336).Within(.001));
            game.CastSkill(2, new Vector2(1400, 360));
            Assert.That(game.State.Player.Position.x, Is.EqualTo(976).Within(.001));
        }

        [Test] public void MageDefenseAugmentReducesDamageOnlyDuringOverload()
        {
            var game = Run("mage"); Grant(game, "magic-r-defense");
            game.State.Ultimate = 100; game.CastSkill(3, game.State.Player.Position); game.DamagePlayer(40);
            Assert.That(game.State.Player.Hp, Is.EqualTo(60).Within(.001));
        }

        [TestCase("swordsman")] [TestCase("archer")] [TestCase("mage")]
        public void EliteRewardNeverOffersAnotherClassAugment(string character)
        {
            for (uint seed = 1; seed <= 12; seed++)
            {
                var game = new GameSimulation(seed, "heal", "haste", character);
                var elite = game.SpawnEnemy("elite", new Vector2(100, 100), 5);
                game.DamageEnemy(elite, 100000); Advance(game, 3.2f);
                Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Augment));
                foreach (var choice in game.State.AugmentChoices)
                {
                    var data = System.Array.Find(Data.SurvivalLegendCatalog.SpecialAugments, a => a.Id == choice.Id);
                    Assert.That(string.IsNullOrEmpty(data.CharacterId) || data.CharacterId == character, Is.True, choice.Id);
                }
            }
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace SurvivalLegend.Tests
{
    public class SimulationParityTests
    {
        static void Advance(GameSimulation game, float seconds)
        {
            int frames = Mathf.CeilToInt(seconds * 60f);
            for (int i = 0; i < frames; i++) game.Step(1f / 60f);
        }

        [Test] public void ArcherStartsWithOriginalStatsAndIndependentCooldowns()
        {
            var game = new GameSimulation(42);
            var p = game.State.Player;
            Assert.That(p.MaxHp, Is.EqualTo(120));
            Assert.That(p.Damage, Is.EqualTo(17.6f).Within(.0001f));
            Assert.That(p.Speed, Is.EqualTo(216));
            Assert.That(p.AttackSpeed, Is.EqualTo(1.7f).Within(.0001f));
            Assert.That(p.Armor, Is.EqualTo(8));
            Assert.That(p.Range, Is.EqualTo(300));
            Assert.That(p.HealthRegen, Is.EqualTo(3));
            Assert.That(game.State.Skills[0].Cooldown, Is.EqualTo(11));
            Assert.That(game.State.Skills[1].Cooldown, Is.EqualTo(7));
            Assert.That(game.State.Skills[2].Cooldown, Is.EqualTo(8));
            Assert.That(game.State.DodgeCharges, Is.EqualTo(2));
        }

        [TestCase(1,3)] [TestCase(2,6)] [TestCase(3,10)] [TestCase(4,15)]
        public void LevelRequirementsAreAdditionalKills(int level, int expected)
        { Assert.That(GameSimulation.KillsForNextLevel(level), Is.EqualTo(expected)); }

        [Test] public void ArmorUsesOriginalDiminishingFormula()
        {
            Assert.That(GameSimulation.MitigatedDamage(100, 100), Is.EqualTo(50).Within(.001));
            Assert.That(GameSimulation.MitigatedDamage(100, -10), Is.EqualTo(100).Within(.001));
        }

        [Test] public void IdleAndOrdinaryMovementDoNotAutoAttack()
        {
            var game = new GameSimulation(42);
            var enemy = game.SpawnEnemy("chaser", game.State.Player.Position + new Vector2(150,0));
            float hp = enemy.Hp;
            Advance(game, .2f);
            game.MoveTo(game.State.Player.Position + new Vector2(0,80));
            Advance(game, .2f);
            Assert.That(enemy.Hp, Is.EqualTo(hp));
            Assert.That(game.State.DamageDealt, Is.EqualTo(0));
        }

        [Test] public void AttackOrderActuallyDamagesTarget()
        {
            var game = new GameSimulation(42);
            game.State.Player.CritChance = 0;
            var enemy = game.SpawnEnemy("chaser", game.State.Player.Position + new Vector2(120,0));
            game.Attack(enemy.Id);
            Advance(game, 1f);
            Assert.That(game.State.DamageDealt, Is.GreaterThan(0));
        }

        [Test] public void NaturalRecoveryIsThreePerCombatSecondAndClamps()
        {
            var game = new GameSimulation(42);
            game.State.Player.Hp = 20;
            Advance(game, 1f);
            Assert.That(game.State.Player.Hp, Is.EqualTo(23).Within(.005));
            game.State.Player.Hp = 119.99f;
            game.Step(1f/60f);
            Assert.That(game.State.Player.Hp, Is.EqualTo(120));
        }

        [Test] public void LifestealAndDamageStatisticsExcludeOverkillAndDeadTargets()
        {
            var game = new GameSimulation(42);
            game.State.Player.Hp = 20;
            game.State.Player.Lifesteal = .1f;
            game.State.Player.CritChance = 0;
            var enemy = game.SpawnEnemy("chaser", new Vector2(100,100));
            enemy.Hp = 30;
            game.DamageEnemy(enemy, 10000);
            game.DamageEnemy(enemy, 10000);
            Assert.That(game.State.Player.Hp, Is.EqualTo(23).Within(.001));
            Assert.That(game.State.DamageDealt, Is.EqualTo(30).Within(.001));
            Assert.That(game.State.Kills, Is.EqualTo(1));
        }

        [Test] public void PauseFreezesCombatCooldownsAndRegeneration()
        {
            var game = new GameSimulation(42);
            game.CastSkill(0, new Vector2(800,720));
            game.State.Player.Hp = 50;
            float cooldown = game.State.Skills[0].Remaining;
            game.TogglePause();
            Advance(game, 2);
            Assert.That(game.State.Time, Is.EqualTo(0));
            Assert.That(game.State.Player.Hp, Is.EqualTo(50));
            Assert.That(game.State.Skills[0].Remaining, Is.EqualTo(cooldown));
        }

        [Test] public void LevelUpFullyHealsThenAugmentSelectionFreezesTime()
        {
            var game = new GameSimulation(42);
            game.State.Player.Hp = 10;
            for (int i=0; i<3; i++) game.DamageEnemy(game.SpawnEnemy("chaser",new Vector2(100,100)),10000);
            game.Step(1f/60f);
            Assert.That(game.State.Level, Is.EqualTo(2));
            Assert.That(game.State.Player.MaxHp, Is.EqualTo(127.2f).Within(.01f));
            Assert.That(game.State.Player.Hp, Is.EqualTo(game.State.Player.MaxHp).Within(.01f));
            Advance(game, 1.6f);
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Augment));
            Assert.That(game.State.AugmentChoices.Count, Is.EqualTo(3));
            float time=game.State.Time;
            Advance(game,1);
            Assert.That(game.State.Time, Is.EqualTo(time));
            game.ChooseAugment(0);
            Assert.That(game.State.History.Count, Is.EqualTo(1));
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Playing));
        }

        [Test] public void ArcherWCreatesSevenProjectilesAndEIsPiercing()
        {
            var game = new GameSimulation(42);
            game.CastSkill(1,new Vector2(1000,720));
            Assert.That(game.State.Projectiles.Count, Is.EqualTo(7));
            Assert.That(game.State.Skills[1].Remaining, Is.GreaterThan(0));
            game.CastSkill(2,new Vector2(1000,720));
            Assert.That(game.State.Projectiles.Exists(p=>p.Pierce), Is.True);
        }

        [Test] public void UltimateRequiresChargeAndConsumesIt()
        {
            var game = new GameSimulation(42);
            game.CastSkill(3,new Vector2(1000,720));
            Assert.That(game.State.Ultimate, Is.EqualTo(0));
            game.State.Ultimate=100;
            game.CastSkill(3,new Vector2(1000,720));
            Assert.That(game.State.Ultimate, Is.EqualTo(0));
        }

        [Test] public void VolleyStartsAtOriginalBowSocketAndKeepsOriginalTravelRange()
        {
            var game=new GameSimulation(42);
            game.CastSkill(1,new Vector2(1000,720));
            var shot=game.State.Projectiles[3];
            Assert.That(shot.Position.x, Is.EqualTo(752.45f).Within(.01f));
            Assert.That(shot.Position.y, Is.EqualTo(720).Within(.01f));
            Assert.That(shot.Life, Is.EqualTo((600-32.45f)/700).Within(.001f));
        }

        [Test] public void DodgePreventsDamageAndSecondUseDoesNotResetRecharge()
        {
            var game = new GameSimulation(42);
            game.Dodge(new Vector2(1000,720));
            game.DamagePlayer(1000);
            Assert.That(game.State.Player.Hp, Is.EqualTo(120));
            Advance(game,.3f);
            float recharge=game.State.DodgeRecharge;
            game.Dodge(new Vector2(1000,720));
            Assert.That(game.State.DodgeCharges, Is.EqualTo(0));
            Assert.That(game.State.DodgeRecharge, Is.EqualTo(recharge).Within(.001));
        }

        [Test] public void DeathFreezesResultsAndDoesNotRegenerate()
        {
            var game = new GameSimulation(42);
            Advance(game,.5f);
            game.DamagePlayer(10000);
            float time=game.State.Time;
            Advance(game,2);
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Results));
            Assert.That(game.State.Time, Is.EqualTo(time));
            Assert.That(game.State.Player.Hp, Is.EqualTo(0));
            Assert.That(game.State.DamageTaken, Is.EqualTo(120).Within(.01));
        }

        [Test] public void DodgeBlocksSkillsAndSpecialCooldownConsumption()
        {
            var game = new GameSimulation(42,"barrier","haste");
            game.Dodge(new Vector2(1000,720));
            game.CastSkill(1,new Vector2(1000,720));
            game.UseSpecial(0,new Vector2(1000,720));
            Assert.That(game.State.Skills[1].Remaining, Is.EqualTo(0));
            Assert.That(game.State.Specials[0].Remaining, Is.EqualTo(0));
            Assert.That(game.State.Projectiles.Count, Is.EqualTo(0));
        }

        [Test] public void EliteDeathAwardsSpecialThenResumesCombatWithSeparateKillCounts()
        {
            var game = new GameSimulation(42);
            var elite=game.SpawnEnemy("elite",new Vector2(100,100),5);
            game.DamageEnemy(elite,100000);
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.EliteDeath));
            Assert.That(game.State.EliteKills, Is.EqualTo(1));
            Assert.That(game.State.Kills - game.State.EliteKills, Is.EqualTo(0));
            Advance(game,3.2f);
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Augment));
            Assert.That(game.State.SpecialReward, Is.True);
            Assert.That(game.State.AugmentChoices.Count, Is.EqualTo(3));
            game.ChooseAugment(0);
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Playing));
            Advance(game,3);
            Assert.That(game.State.Enemies.Exists(e=>e.Kind!="elite"&&e.Hp>0), Is.True);
        }

        [Test] public void SameSeedAndCommandsProduceSameCombatOutcome()
        {
            var a=new GameSimulation(987); var b=new GameSimulation(987);
            a.AttackMove(new Vector2(1200,1000)); b.AttackMove(new Vector2(1200,1000));
            Advance(a,10); Advance(b,10);
            Assert.That(a.State.Enemies.Count, Is.EqualTo(b.State.Enemies.Count));
            Assert.That(a.State.Player.Position, Is.EqualTo(b.State.Player.Position));
            Assert.That(a.State.Player.Hp, Is.EqualTo(b.State.Player.Hp));
            Assert.That(a.State.Kills, Is.EqualTo(b.State.Kills));
            Assert.That(a.State.Seed, Is.EqualTo(b.State.Seed));
        }

        [Test] public void LaterEliteMilestoneWaitsForCurrentEliteAndPendingNormalReward()
        {
            var game=new GameSimulation(42);
            game.State.Level=4; game.State.Xp=game.State.NextXp;
            game.Step(1f/60f);
            var first=game.State.Enemies.Find(e=>e.Kind=="elite"&&e.Hp>0);
            Assert.That(first, Is.Not.Null);
            Advance(game,1.6f); game.ChooseAugment(0);
            game.State.Level=9; game.State.Xp=game.State.NextXp;
            game.Step(1f/60f);
            game.DamageEnemy(first,100000);
            Advance(game,3.2f); game.ChooseAugment(0);
            Assert.That(game.State.SpecialReward, Is.False);
            Advance(game,.7f);
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Augment));
            game.ChooseAugment(0);
            Assert.That(game.State.Enemies.FindAll(e=>e.Kind=="elite"&&e.Hp>0).Count, Is.EqualTo(1));
            Assert.That(game.State.Enemies.Find(e=>e.Kind=="elite"&&e.Hp>0).Level, Is.EqualTo(10));
        }
    }
}

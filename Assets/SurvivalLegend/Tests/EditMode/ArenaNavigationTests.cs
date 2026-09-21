using NUnit.Framework;
using UnityEngine;
using SurvivalLegend.Data;

namespace SurvivalLegend.Tests
{
    /// <summary>Simulation-side guarantees for authored logical-space solids.</summary>
    public class ArenaNavigationTests
    {
        static readonly ObstacleData[] CentralRock =
        {
            new ObstacleData("central-rock", new Vector2(720, 720), 120)
        };

        static GameSimulation RunWith(ObstacleData[] obstacles, uint seed = 11, string character = null, string specialD = null)
            => new GameSimulation(seed, specialD, specialD == "blackhole" ? "haste" : null, character,
                GameContentSnapshot.CreateDefault().WithObstacles(obstacles));

        static void AssertWalkable(GameSimulation game, Vector2 position, float radius)
            => Assert.That(game.Navigation.IsWalkable(position, radius), Is.True,
                "Actor entered an authored solid or left its logical clearance bounds.");

        [Test]
        public void SnapshotSortsAndDeepCopiesObstacleDefinitions()
        {
            var source = new[]
            {
                new ObstacleData("zeta", new Vector2(900, 720), 55),
                new ObstacleData("alpha", new Vector2(540, 720), 65)
            };
            var snapshot = GameContentSnapshot.CreateDefault().WithObstacles(source);
            source[0] = new ObstacleData("changed", Vector2.zero, 1);

            Assert.That(snapshot.Obstacles.Count, Is.EqualTo(2));
            Assert.That(snapshot.Obstacles[0].Id, Is.EqualTo("alpha"));
            Assert.That(snapshot.Obstacles[1].Id, Is.EqualTo("zeta"));
            Assert.That(snapshot.Clone().Obstacles[0].Position, Is.EqualTo(new Vector2(540, 720)));
        }

        [Test]
        public void ResolveAndSweepUseActorRadiusAndCannotTunnelThroughSolid()
        {
            var game = RunWith(CentralRock);
            var navigation = game.Navigation;
            const float radius = 15;
            var resolved = navigation.ResolvePosition(new Vector2(720, 720), radius);
            var swept = navigation.SweepMove(new Vector2(260, 720), new Vector2(1180, 720), radius);

            Assert.That(navigation.IsWalkable(resolved, radius), Is.True);
            Assert.That(navigation.IsWalkable(swept, radius), Is.True);
            Assert.That(swept.x, Is.LessThan(720 - 120 - radius));
            Assert.That(navigation.HasLineOfSight(new Vector2(260, 720), new Vector2(1180, 720), radius), Is.False);
        }

        [Test]
        public void PlayerRoutesAroundSolidAndNeverOverlapsIt()
        {
            var game = RunWith(CentralRock);
            game.State.Player.Position = new Vector2(260, 720);
            game.State.Player.Speed = 1000;
            var goal = new Vector2(1180, 720);
            game.MoveTo(goal);
            bool detoured = false;

            for (var i = 0; i < 80 && game.State.Order != "idle"; i++)
            {
                game.Step(.05f);
                AssertWalkable(game, game.State.Player.Position, game.Content.Config.PlayerRadius);
                detoured |= Mathf.Abs(game.State.Player.Position.y - goal.y) > 8;
            }

            Assert.That(detoured, Is.True, "The destination is behind a solid, so a direct-line move is invalid.");
            Assert.That(Vector2.Distance(game.State.Player.Position, goal), Is.LessThan(.6f));
            Assert.That(game.State.Order, Is.EqualTo("idle"));
        }

        [Test]
        public void EnemyChasesAroundSolidAndSpawnInsideSolidIsResolvedWithoutRngUse()
        {
            var game = RunWith(CentralRock, 79);
            var baseline = new GameSimulation(79);
            game.State.Player.Position = new Vector2(1180, 720);
            var enemy = game.SpawnEnemy("chaser", new Vector2(720, 720));
            baseline.SpawnEnemy("chaser", new Vector2(720, 720));
            Assert.That(game.State.Seed, Is.EqualTo(baseline.State.Seed), "Obstacle resolution must not add gameplay RNG draws.");
            AssertWalkable(game, enemy.Position, enemy.Radius);

            enemy.Position = new Vector2(260, 720);
            enemy.Speed = 1000;
            bool detoured = false;
            for (var i = 0; i < 80; i++)
            {
                game.Step(.05f);
                AssertWalkable(game, enemy.Position, enemy.Radius);
                detoured |= Mathf.Abs(enemy.Position.y - 720) > 8;
                if (enemy.Position.x > 1000) break;
            }

            Assert.That(detoured, Is.True);
            Assert.That(enemy.Position.x, Is.GreaterThan(1000));
        }

        [Test]
        public void NaturalEliteSpawnStaysOnSafeArenaEdge()
        {
            var game = RunWith(CentralRock, 93);
            game.State.Level = 4;
            game.State.Xp = game.State.NextXp;
            game.Step(.01f);

            var elite = game.State.Enemies.Find(enemy => game.IsElite(enemy.Kind));
            Assert.That(elite, Is.Not.Null);
            AssertWalkable(game, elite.Position, elite.Radius);
            float inset = Mathf.Max(game.Content.Config.ArenaInset, elite.Radius);
            bool onEdge = Mathf.Abs(elite.Position.x - inset) < .01f ||
                          Mathf.Abs(elite.Position.x - (game.Content.Config.ArenaWidth - inset)) < .01f ||
                          Mathf.Abs(elite.Position.y - inset) < .01f ||
                          Mathf.Abs(elite.Position.y - (game.Content.Config.ArenaHeight - inset)) < .01f;
            Assert.That(onEdge, Is.True);
        }

        [Test]
        public void DodgeBlinkAndPullAreSweptInsteadOfPassingThroughSolids()
        {
            var dodgeRock = new[] { new ObstacleData("dodge-wall", new Vector2(510, 720), 60) };
            var dodge = RunWith(dodgeRock);
            dodge.State.Player.Position = new Vector2(300, 720);
            Assert.That(dodge.Dodge(new Vector2(1000, 720)), Is.True);
            for (var i = 0; i < 8; i++) { dodge.Step(.05f); AssertWalkable(dodge, dodge.State.Player.Position, dodge.Content.Config.PlayerRadius); }
            Assert.That(dodge.State.Player.Position.x, Is.LessThan(510 - 60 - dodge.Content.Config.PlayerRadius + .5f));

            var blinkRock = new[] { new ObstacleData("blink-wall", new Vector2(550, 720), 75) };
            var blink = RunWith(blinkRock, character: "mage");
            blink.State.Player.Position = new Vector2(300, 720);
            Assert.That(blink.CastSkill(2, new Vector2(1100, 720)), Is.True);
            AssertWalkable(blink, blink.State.Player.Position, blink.Content.Config.PlayerRadius);
            Assert.That(blink.State.Player.Position.x, Is.LessThan(550 - 75 - blink.Content.Config.PlayerRadius + .5f));

            var pullRock = new[] { new ObstacleData("pull-wall", new Vector2(650, 720), 60) };
            var pull = RunWith(pullRock, 92, specialD: "blackhole");
            pull.State.Player.Position = new Vector2(300, 720);
            var pulled = pull.SpawnEnemy("chaser", new Vector2(500, 720));
            pulled.Speed = 0;
            Assert.That(pull.UseSpecial(0, new Vector2(1000, 720)), Is.True);
            for (var i = 0; i < 20; i++) { pull.Step(.05f); AssertWalkable(pull, pulled.Position, pulled.Radius); }
            Assert.That(pulled.Position.x, Is.LessThan(650 - 60 - pulled.Radius + .5f));
        }

        [Test]
        public void NoObstacleNavigationKeepsDirectMovementFastPath()
        {
            var game = new GameSimulation(123);
            var start = new Vector2(300, 720);
            var target = new Vector2(900, 720);
            game.State.Player.Position = start;

            Assert.That(game.Navigation.HasObstacles, Is.False);
            Assert.That(game.Navigation.NextWaypoint(start, target, game.Content.Config.PlayerRadius), Is.EqualTo(target));
            Assert.That(game.Navigation.SweepMove(start, target, game.Content.Config.PlayerRadius), Is.EqualTo(target));
            Assert.That(game.Navigation.FieldBuildCount, Is.Zero);
        }

        [Test]
        public void ObstacleOrderingDoesNotChangeRouteOrGameplaySeed()
        {
            var ordered = new[]
            {
                new ObstacleData("a", new Vector2(720, 720), 120),
                new ObstacleData("b", new Vector2(720, 980), 55)
            };
            var reversed = new[] { ordered[1], ordered[0] };
            var left = RunWith(ordered, 451);
            var right = RunWith(reversed, 451);
            left.State.Player.Position = right.State.Player.Position = new Vector2(260, 720);
            left.MoveTo(new Vector2(1180, 720)); right.MoveTo(new Vector2(1180, 720));

            for (var i = 0; i < 40; i++) { left.Step(.05f); right.Step(.05f); }

            Assert.That(left.State.Seed, Is.EqualTo(right.State.Seed));
            Assert.That(Vector2.Distance(left.State.Player.Position, right.State.Player.Position), Is.LessThan(.001f));
            Assert.That(left.Navigation.FieldBuildCount, Is.EqualTo(right.Navigation.FieldBuildCount));
        }
    }
}

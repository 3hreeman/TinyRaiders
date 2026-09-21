using NUnit.Framework;
using UnityEngine;
using SurvivalLegend.World;

namespace SurvivalLegend.Tests
{
    public class FogVisibilityGridTests
    {
        [Test]
        public void ExpandedMapUsesStableResolutionAndLogicalTopRow()
        {
            var grid = new FogVisibilityGrid(new Vector2(3600, 3600), 16);
            grid.Reveal(new Vector2(112.5f, 112.5f), 90);

            Assert.That(grid.LogicalSize, Is.EqualTo(new Vector2(3600, 3600)));
            Assert.That(grid.Width, Is.EqualTo(16));
            Assert.That(grid.Height, Is.EqualTo(16));
            Assert.That(grid.GetAlpha(0, 0), Is.LessThan(.1f), "Logical (0,0) is the top-left map cell.");
            Assert.That(grid.GetAlpha(0, grid.Height - 1), Is.GreaterThan(.95f));
        }

        [Test]
        public void MovingVisionKeepsExplorationButOnlyCurrentRadiusVisible()
        {
            var grid = new FogVisibilityGrid(new Vector2(3600, 3600), 32);
            var first = new Vector2(420, 420);
            var second = new Vector2(3180, 3180);

            grid.Reveal(first, 300);
            Assert.That(grid.IsVisible(first), Is.True);
            Assert.That(grid.IsExplored(first), Is.True);
            Assert.That(grid.IsExplored(second), Is.False);

            grid.Reveal(second, 300);
            Assert.That(grid.IsVisible(first), Is.False);
            Assert.That(grid.IsExplored(first), Is.True, "Previously revealed ground stays as explored memory.");
            Assert.That(grid.IsVisible(second), Is.True);
            Assert.That(grid.IsExplored(second), Is.True);
            Assert.That(grid.IsVisible(new Vector2(-1, 420)), Is.False);
            Assert.That(grid.IsExplored(new Vector2(3601, 420)), Is.False);
        }

        [Test]
        public void ClearRemovesBothCurrentVisibilityAndExploration()
        {
            var grid = new FogVisibilityGrid(new Vector2(3600, 3600), 16);
            var point = new Vector2(900, 900);
            grid.Reveal(point, 500);
            Assert.That(grid.IsExplored(point), Is.True);

            grid.Clear();

            Assert.That(grid.IsVisible(point), Is.False);
            Assert.That(grid.IsExplored(point), Is.False);
            Assert.That(grid.GetAlpha(4, 4), Is.EqualTo(1).Within(.0001f));
        }

        [Test]
        public void AlphaSeparatesVisibleExploredAndUnseenCells()
        {
            var grid = new FogVisibilityGrid(new Vector2(3600, 3600), 16);
            grid.Reveal(new Vector2(337.5f, 337.5f), 180);
            grid.Reveal(new Vector2(3262.5f, 3262.5f), 180);

            float explored = grid.GetAlpha(1, 1);
            float visible = grid.GetAlpha(14, 14);
            float unseen = grid.GetAlpha(8, 8);
            Assert.That(visible, Is.LessThan(explored));
            Assert.That(explored, Is.LessThan(unseen));
        }
    }
}

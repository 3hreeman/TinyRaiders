using NUnit.Framework;
using UnityEngine;
using SurvivalLegend.World;

namespace SurvivalLegend.Tests
{
    /// <summary>The gameplay arena remains a 1440-square even when its rendered field is rectangular.</summary>
    public class WorldProjectionTests
    {
        [TestCase(0, 0, 40, 45)]
        [TestCase(1440, 0, 1240, 45)]
        [TestCase(0, 1440, 40, 515)]
        [TestCase(1440, 1440, 1240, 515)]
        [TestCase(720, 720, 640, 280)]
        public void LogicalArenaMapsToAuthoredFieldRectangle(float x, float y, float expectedX, float expectedY)
        {
            var logical = new Vector2(x, y);
            var view = WorldProjection.WorldToView(logical);

            Assert.That(view.x, Is.EqualTo(expectedX).Within(.001f));
            Assert.That(view.y, Is.EqualTo(expectedY).Within(.001f));
            Assert.That(Vector2.Distance(WorldProjection.ViewToWorld(view), logical), Is.LessThan(.001f));
            Assert.That(Vector2.Distance(WorldProjection.SceneToWorld(WorldProjection.WorldToScene(logical)), logical),
                Is.LessThan(.001f));
        }

        [Test]
        public void LogicalDirectionsKeepCardinalScreenAxes()
        {
            var east = WorldProjection.DirectionToScene(Vector2.right);
            var north = WorldProjection.DirectionToScene(Vector2.up);
            var diagonal = WorldProjection.DirectionToScene(new Vector2(1, 1));

            Assert.That(east.x, Is.GreaterThan(0));
            Assert.That(east.y, Is.EqualTo(0).Within(.00001f));
            Assert.That(north.x, Is.EqualTo(0).Within(.00001f));
            Assert.That(north.y, Is.LessThan(0));
            Assert.That(diagonal.x, Is.GreaterThan(0));
            Assert.That(diagonal.y, Is.LessThan(0));
        }
    }
}

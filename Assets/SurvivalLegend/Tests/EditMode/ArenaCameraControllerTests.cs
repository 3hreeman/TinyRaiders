using NUnit.Framework;
using UnityEngine;
using SurvivalLegend.World;

namespace SurvivalLegend.Tests
{
    /// <summary>Camera movement is presentation-only: it must keep logical input and map bounds coherent.</summary>
    public class ArenaCameraControllerTests
    {
        static ArenaCameraController Create(out GameObject root, out Camera camera)
        {
            root = new GameObject("Camera test host");
            camera = root.AddComponent<Camera>();
            var controller = root.AddComponent<ArenaCameraController>();
            controller.EditorSet(null, camera);
            return controller;
        }

        static void AssertInsideLogicalMap(Rect visible)
        {
            Assert.That(visible.xMin, Is.GreaterThanOrEqualTo(-.001f));
            Assert.That(visible.yMin, Is.GreaterThanOrEqualTo(-.001f));
            Assert.That(visible.xMax, Is.LessThanOrEqualTo(1440.001f));
            Assert.That(visible.yMax, Is.LessThanOrEqualTo(1440.001f));
        }

        [TestCase(.01f)]
        [TestCase(1.6875f)]
        [TestCase(3.375f)]
        [TestCase(5.875f)]
        [TestCase(100f)]
        public void PanAndZoomNeverExposeOutsideFallbackLogicalMap(float requestedZoom)
        {
            var controller = Create(out var root, out _);
            try
            {
                controller.SetZoom(requestedZoom);
                controller.PanToLogical(new Vector2(-10000, 10000));
                AssertInsideLogicalMap(controller.VisibleLogicalRect);
                controller.PanScene(new Vector2(10000, -10000));
                AssertInsideLogicalMap(controller.VisibleLogicalRect);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ProgrammaticFollowRecentersAfterFreePan()
        {
            var controller = Create(out var root, out _);
            try
            {
                controller.PanToLogical(new Vector2(1100, 1100));
                Assert.That(controller.IsFollowing, Is.False);
                controller.FocusPlayer();
                Assert.That(controller.IsFollowing, Is.True);
                Assert.That(Vector2.Distance(controller.VisibleLogicalRect.center, new Vector2(720, 720)), Is.LessThan(.01f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CameraScreenToWorldRoundTripsUnderFreePanAndZoom()
        {
            var controller = Create(out var root, out var camera);
            try
            {
                controller.SetZoom(1.3f);
                controller.PanToLogical(new Vector2(1040, 1040));
                var logical = controller.VisibleLogicalRect.center;
                var screen = camera.WorldToScreenPoint(WorldProjection.WorldToScene(logical));
                var roundTrip = WorldProjection.ScreenToWorld(camera, new Vector2(screen.x, screen.y));

                Assert.That(Vector2.Distance(roundTrip, logical), Is.LessThan(.001f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(45, 300, -1, 0)] // left edge of the playable field
        [TestCase(5, 300, -1, 0)]  // left edge of the full letterboxed frame
        [TestCase(640, 5, 0, -1)]
        [TestCase(1275, 715, 1, 1)]
        [TestCase(640, 215, 0, 0)] // former field bottom is now interior, behind the HUD
        public void EdgePanRecognizesFieldAndLetterboxBorders(float x, float y, float expectedX, float expectedY)
        {
            var controller = Create(out var root, out _);
            try
            {
                Assert.That(controller.EdgePanDirection(new Vector2(x, y), 1280, 720),
                    Is.EqualTo(new Vector2(expectedX, expectedY)));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(1280, 720, 40, 0, 1200, 675)]
        [TestCase(1920, 1080, 60, 0, 1800, 1012.5f)]
        [TestCase(1600, 900, 50, 0, 1500, 843.75f)]
        public void FieldViewportTracksLetterboxedResize(float width, float height, float x, float y, float viewportWidth, float viewportHeight)
        {
            var rect = WorldProjection.FieldViewportPixels(width, height);
            Assert.That(rect.x, Is.EqualTo(x).Within(.001f));
            Assert.That(rect.y, Is.EqualTo(y).Within(.001f));
            Assert.That(rect.width, Is.EqualTo(viewportWidth).Within(.001f));
            Assert.That(rect.height, Is.EqualTo(viewportHeight).Within(.001f));
        }
    }
}

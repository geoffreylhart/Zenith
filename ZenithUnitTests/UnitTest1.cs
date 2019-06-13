using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zenith.EditorGameComponents;
using Zenith.MathHelpers;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace ZenithUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestPlaneDistance()
        {
            Plane plane = new Plane(new Vector3d(-0.5 + 5, -0.5 - 5, 20), new Vector3d(1, 1, 0));
            double distance = plane.GetDistanceFromPoint(new Vector3d(0, 0, 0));
            AssertIsClose(distance, 0.5 * Math.Sqrt(2));
            Circle3 intersection = plane.GetUnitSphereIntersection();
            AssertIsClose(intersection.radius, 0.5 * Math.Sqrt(2));
        }

        private void AssertIsClose(double a, double b)
        {
            Assert.IsTrue(Math.Abs(a - b) < 0.001);
        }

        // make sure our cube coordinates turn out as expected
        [TestMethod]
        public void TestCubeFaces()
        {
            var front = new CubeSector(CubeSector.CubeSectorFace.FRONT, 0, 0, 0);
            Vector3d normal = front.sectorFace.GetFaceNormal();
            Vector3d up = front.sectorFace.GetFaceUpDirection();
            Vector3d right = front.sectorFace.GetFaceRightDirection();
            var longLat = new LongLat(Math.PI / 4, 0).ToSphereVector(); // rightmost of the front cubeface
            AssertIsClose(front.GetRel(normal, right, longLat), 1);
        }
    }
}

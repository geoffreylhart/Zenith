using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zenith.MathHelpers;
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
    }
}

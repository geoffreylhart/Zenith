using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;
using Zenith.ZMath;

namespace ZenithUnitTests
{
    [TestClass]
    public class PerformanceTests
    {
        [TestMethod]
        public void TestPlaneDistance2()
        {
            LongLat longLat = new LongLat(-87.3294527 * Math.PI / 180, 30.4668536 * Math.PI / 180);
            CubeSector root = new CubeSector(CubeSector.CubeSectorFace.LEFT, 0, 0, 0);
            Vector2d relativeCoord = root.ProjectToLocalCoordinates(longLat.ToSphereVector());
            ISector sector = root.GetSectorAt(relativeCoord.X, relativeCoord.Y, 8);


            Plane plane = new Plane(new Vector3d(-0.5 + 5, -0.5 - 5, 20), new Vector3d(1, 1, 0));
            double distance = plane.GetDistanceFromPoint(new Vector3d(0, 0, 0));
            ZAssert.AssertIsClose(distance, 0.5 * Math.Sqrt(2));
            Circle3 intersection = plane.GetUnitSphereIntersection();
            ZAssert.AssertIsClose(intersection.radius, 0.5 * Math.Sqrt(2));
        }
    }
}

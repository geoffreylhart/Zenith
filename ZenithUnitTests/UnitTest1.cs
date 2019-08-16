using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zenith.EditorGameComponents;
using Zenith.MathHelpers;
using Zenith.ZGeom;
using Zenith.ZMath;
using static Zenith.ZMath.CubeSector;

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
            ZAssert.AssertIsClose(distance, 0.5 * Math.Sqrt(2));
            Circle3 intersection = plane.GetUnitSphereIntersection();
            ZAssert.AssertIsClose(intersection.radius, 0.5 * Math.Sqrt(2));
        }

        // make sure our cube coordinates turn out as expected
        [TestMethod]
        public void TestCubeFaces()
        {
            // do basic front ones
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(Math.PI / 4, 0), true, 0.5);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(Math.PI / 8, 0), true, 0.25);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(0, 0), true, 0);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(-Math.PI / 8, 0), true, -0.25);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(-Math.PI / 4, 0), true, -0.5);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(0, Math.PI / 4), false, -0.5);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(0, Math.PI / 8), false, -0.25);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(0, 0), false, 0);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(0, -Math.PI / 8), false, 0.25);
            DoCubeFaceTest(CubeSectorFace.FRONT, new LongLat(0, -Math.PI / 4), false, 0.5);
            // do the same up top, sure
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(Math.PI / 2, Math.PI / 4), true, 0.5);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(Math.PI / 2, 3 * Math.PI / 8), true, 0.25);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(0, Math.PI / 2), true, 0);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(-Math.PI / 2, 3 * Math.PI / 8), true, -0.25);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(-Math.PI / 2, Math.PI / 4), true, -0.5);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(-Math.PI, Math.PI / 4), false, -0.5);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(-Math.PI, 3 * Math.PI / 8), false, -0.25);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(0, Math.PI / 2), false, 0);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(0, 3 * Math.PI / 8), false, 0.25);
            DoCubeFaceTest(CubeSectorFace.TOP, new LongLat(0, Math.PI / 4), false, 0.5);
            // check for values outside 1 and -1
            DoCubeFaceTest(CubeSectorFace.LEFT, new LongLat(Math.PI / 8, 0), true, 1.25);
            DoCubeFaceTest(CubeSectorFace.RIGHT, new LongLat(Math.PI / 8, 0), true, -0.75);
            DoCubeFaceTest(CubeSectorFace.BACK, new LongLat(Math.PI / 8, 0), true, -1.75);
        }

        private void DoCubeFaceTest(CubeSectorFace face, LongLat longLat, bool doXNotY, double expectedAnswer)
        {
            var front = new CubeSector(face, 0, 0, 0);
            Vector3d normal = front.sectorFace.GetFaceNormal();
            Vector3d down = front.sectorFace.GetFaceDownDirection();
            Vector3d right = front.sectorFace.GetFaceRightDirection();
            if (doXNotY)
            {
                ZAssert.AssertIsClose(front.GetRel(normal, right, longLat.ToSphereVector()), expectedAnswer);
            }
            else
            {
                ZAssert.AssertIsClose(front.GetRel(normal, down, longLat.ToSphereVector()), expectedAnswer);
            }
        }

        [TestMethod]
        public void TestSectorPointCoverage()
        {
            Random r = new Random();
            foreach (var manager in new ISectorManager[] { new MercatorSectorManager(), new CubeSectorManager() })
            {
                var sectors = new List<ISector>();
                foreach (var root in manager.GetTopmostOSMSectors())
                {
                    sectors.AddRange(root.GetChildrenAtLevel(3));
                }
                for (int i = 0; i < 1000; i++)
                {
                    LongLat longLat;
                    if (manager is MercatorSectorManager)
                    {
                        // mercator can't show poles
                        longLat = new LongLat(r.NextDouble() * 2 * Math.PI - Math.PI, (r.NextDouble() - 0.5) * 85.051129 * 2 * Math.PI / 180);
                    }
                    else
                    {
                        longLat = new LongLat(r.NextDouble() * 2 * Math.PI - Math.PI, r.NextDouble() * Math.PI - Math.PI / 2);
                    }
                    var sectorsThatCover = sectors.Where(x => x.ContainsLongLat(longLat)).ToList();
                    Assert.AreEqual(sectorsThatCover.Count, 1);
                }
            }
        }
    }
}

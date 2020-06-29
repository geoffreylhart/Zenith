using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith;
using Zenith.LibraryWrappers.OSM;
using Zenith.MathHelpers;
using Zenith.ZGraphics.GraphicsBuffers;
using Zenith.ZMath;

namespace ZenithUnitTests
{
    [TestClass]
    public class PerformanceTests
    {
        [TestMethod]
        public void TestSectorLoadPerformance()
        {
            LongLat longLat = new LongLat(-87.3294527 * Math.PI / 180, 30.4668536 * Math.PI / 180);
            CubeSector root = new CubeSector(CubeSector.CubeSectorFace.LEFT, 0, 0, 0);
            Vector2d relativeCoord = root.ProjectToLocalCoordinates(longLat.ToSphereVector());
            ISector sector = root.GetSectorAt(relativeCoord.X, relativeCoord.Y, 8);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
            buffer.LoadLinesFromFile();
            double loadTimeSecs = sw.Elapsed.TotalSeconds; // 0.842 secs (1.781 tablet)
            sw.Restart();
            buffer.GenerateVertices();
            double vertSecs = sw.Elapsed.TotalSeconds; // 0.404 secs (0.773 tablet)
        }

        // 20 secs as of 2/25/2020 on desktop
        [TestMethod]
        public void ToughSectorsTest()
        {
            // current errors in LE,X=2,Y=0,Z=2 is 6/4096 ~1 have issues that aren't red
            Constants.DEBUG_MODE = true;
            // add a tough sector whenever one misses a crash issue
            List<ISector> sectors = new List<ISector>();
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 128, 43, 8));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 129, 44, 8));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 128, 42, 8));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 135, 41, 8)); // "topological inconsistency" during tesselation - aka those wingdings TODO: currently can't test this
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 128, 35, 8)); // has two ways that are deep copies of each other
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 143, 30, 8)); // has vertices exactly on the border of the sector
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 166, 23, 8)); // results in duplicate intersections
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 143, 3, 8)); // error when too agressive with collinear points
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 130, 20, 8)); // fails due to duplicate nodes within a single way - aka node 5007634875
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 151, 5, 8)); // fails because of multiple ways with lines exactly along border of sector joining together
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 141, 30, 8)); // ?
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 148, 13, 8)); // another tiny thin polygon
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 137, 25, 8)); // broke because we weren't even checking intersections on relations...?
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 145, 36, 8)); // broke because of a trisected relation

            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 155, 4, 8)); // a lake inside of a lake that should've been deleted triggers more issues when another shape intersects with both (and they disagree on if it should be deleted/alive)
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 139, 24, 8)); // an inner is misidentified as an outer
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 162, 58, 8)); // had a coastline island that was the wrong direction and intersected with the border

            foreach (var sector in sectors)
            {
                ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
                buffer.LoadLinesFromFile();
                buffer.GenerateVertices();
                buffer.GenerateBuffers(null);
            }
        }

        [TestMethod]
        public void TestParallelPerformance()
        {
            LongLat longLat = new LongLat(-87.3294527 * Math.PI / 180, 30.4668536 * Math.PI / 180);
            CubeSector root = new CubeSector(CubeSector.CubeSectorFace.LEFT, 0, 0, 0);
            Vector2d relativeCoord = root.ProjectToLocalCoordinates(longLat.ToSphereVector());
            List<ISector> sectors = root.GetSectorAt(relativeCoord.X, relativeCoord.Y, 6).GetChildrenAtLevel(8);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var sector in sectors)
            {
                ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
                buffer.LoadLinesFromFile();
                buffer.GenerateVertices();
                buffer.Dispose();
            }
            double sequentialSecs = sw.Elapsed.TotalSeconds; // 5.585 secs
            sw.Restart();
            Parallel.ForEach(sectors, sector =>
            {
                ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
                buffer.LoadLinesFromFile();
                buffer.GenerateVertices();
                buffer.Dispose();
            });
            double parallelSecs = sw.Elapsed.TotalSeconds; // 5.056 secs
            double speedMultiplier = sequentialSecs / parallelSecs; // 1.105 (seems to vary between 1.7 at highest and 1.1 at lowest, not the best multiplier but could still be worthwhile)
        }
    }
}

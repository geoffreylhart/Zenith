using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // add a tough sector whenever one misses a crash issue
            List<ISector> sectors = new List<ISector>();
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 128, 43, 8));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 129, 44, 8));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 128, 42, 8));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 135, 41, 8)); // "topological inconsistency" during tesselation - aka those wingdings TODO: currently can't test this

            foreach (var sector in sectors)
            {
                ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
                buffer.LoadLinesFromFile();
                buffer.GenerateVertices();
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

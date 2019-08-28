using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            double loadTimeSecs = sw.Elapsed.TotalSeconds; // 0.842 secs
            sw.Restart();
            buffer.GenerateVertices();
            double vertSecs = sw.Elapsed.TotalSeconds; // 0.404 secs
        }

        [TestMethod]
        public void TestSectorSize()
        {
            LongLat longLat = new LongLat(-87.3294527 * Math.PI / 180, 30.4668536 * Math.PI / 180);
            CubeSector root = new CubeSector(CubeSector.CubeSectorFace.LEFT, 0, 0, 0);
            Vector2d relativeCoord = root.ProjectToLocalCoordinates(longLat.ToSphereVector());
            ISector sector = root.GetSectorAt(relativeCoord.X, relativeCoord.Y, 8);
            string path = OSMPaths.GetSectorPath(sector);
            long sectorLoadSize = new FileInfo(path).Length; // osm.pbf is 2,316,926 bytes
            ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
            buffer.LoadLinesFromFile();
            buffer.GenerateVertices();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            byte[] verticesBytes = buffer.GetVerticesBytes();
            long verticesSize = verticesBytes.Length; // is 3,195,295 bytes
            double writeSecs = sw.Elapsed.TotalSeconds; // 1.313 secs
            sw.Restart();
            buffer.SetVerticesFromBytes(verticesBytes);
            double readSecs = sw.Elapsed.TotalSeconds; // 2.292 secs
            // rendered image is 230,981 bytes
        }

        [TestMethod]
        public void TestReadWrite()
        {
            long num = 1000;
            string str = "geoffrey";
            double numD = Math.PI;
            byte[] written;
            using (var memStream = new MemoryStream())
            {
                OSMReader.WriteVarInt(memStream, num);
                OSMReader.WriteString(memStream, str);
                OSMReader.WriteDouble(memStream, numD);
                written = memStream.ToArray();
            }
            using (var memStream = new MemoryStream(written))
            {
                long readNum = OSMReader.ReadVarInt(memStream);
                string readStr = OSMReader.ReadString(memStream);
                double readNumD = OSMReader.ReadDouble(memStream);
                Assert.AreEqual(readNum, num);
                Assert.AreEqual(readStr, str);
                Assert.AreEqual(readNumD, numD);
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

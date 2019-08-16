using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            double loadTimeSecs = sw.Elapsed.TotalSeconds;
            sw.Restart();
            buffer.GenerateVertices();
            double vertSecs = sw.Elapsed.TotalSeconds;
            Assert.IsTrue(loadTimeSecs < 2);
            Assert.IsTrue(vertSecs < 0.5);
        }
    }
}

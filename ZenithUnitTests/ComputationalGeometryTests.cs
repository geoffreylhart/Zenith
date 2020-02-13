using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace ZenithUnitTests
{
    [TestClass]
    public class ComputationalGeometryTests
    {
        [TestMethod]
        public void BasicTest()
        {
            var blobs = new BlobCollection();
            TestAddAndSubtractAndScale(2, 1, 0, 1, blobs, 4, 3, false); // test with 2nd square inside corner of 1st
            TestAddAndSubtractAndScale(2, 2, 0, 1, blobs, 5, 4, false); // test test with 2nd square just outside/against corner of 1st
            TestAddAndSubtractAndScale(3, 2, 1, 1, blobs, 9, 8, false); // test with 2nd square inside side of 1st
            TestAddAndSubtractAndScale(3, 3, 1, 1, blobs, 10, 9, false); // test with 2nd square just outside/against side of 1st
            TestAddAndSubtractAndScale(4, 3, 1, 2, blobs, 18, 14, false); // test with 2nd square overlapping side
            TestAddAndSubtractAndScale(3, 1, 1, 1, blobs, 9, 8, false); // test donut TODO: fix the add test for this (though the tesselator does it for us)
            // TestAddAndSubtractAndScale(2, 1, 0, 1, blobs, 4, 3, true);
            TestAddAndSubtractAndScale(2, 2, 0, 1, blobs, 5, 4, true);
            // TestAddAndSubtractAndScale(3, 2, 1, 1, blobs, 9, 8, true);
            TestAddAndSubtractAndScale(3, 3, 1, 1, blobs, 10, 9, true);
            TestAddAndSubtractAndScale(4, 3, 1, 2, blobs, 18, 14, true);
        }

        private static void TestAddAndSubtractAndScale(int size1, int offsetX, int offsetY, int size2, BlobCollection blobs, int addArea, int subArea, bool testOpen)
        {
            TestAddAndSubtract(size1, offsetX, offsetY, size2, blobs, addArea, subArea, testOpen);
            TestAddAndSubtract(size1 * 5, offsetX * 5, offsetY * 5, size2 * 5, blobs, addArea * 25, subArea * 25, testOpen);
        }

        private static void TestAddAndSubtract(int size1, int offsetX, int offsetY, int size2, BlobCollection blobs, int addArea, int subArea, bool testOpen)
        {
            if (!testOpen)
            {
                SectorConstrainedOSMAreaGraph square1 = MakeRect(blobs.nodes, 0, 0, size1, size1);
                SectorConstrainedOSMAreaGraph square2 = MakeRect(blobs.nodes, offsetX, offsetY, offsetX + size2, offsetY + size2);
                //if (square1.Clone().Add(square2, blobs).Area(blobs) != addArea) throw new NotImplementedException();
                //if (square1.Clone().Subtract(square2, blobs).Area(blobs) != subArea) throw new NotImplementedException();
                double area1 = GetArea(square1.Clone().Add(square2, blobs).Finalize(blobs).GetTesselationVertices(Color.White));
                double area2 = GetArea(square1.Clone().Subtract(square2, blobs).Finalize(blobs).GetTesselationVertices(Color.White));
                if (area1 != addArea) throw new NotImplementedException();
                if (area2 != subArea) throw new NotImplementedException();
            }
            else
            {
                // test some coastline stuff
                SectorConstrainedOSMAreaGraph square1 = MakeOpenRect(blobs.nodes, 0, 0, size1, size1, true); // leave left open
                SectorConstrainedOSMAreaGraph square2 = MakeOpenRect(blobs.nodes, offsetX, offsetY, offsetX + size2, offsetY + size2, false); // leave right open
                double area1 = GetArea(square1.Clone().Add(square2, blobs).Finalize(blobs).GetTesselationVertices(Color.White));
                double area2 = GetArea(square1.Clone().Subtract(square2, blobs).Finalize(blobs).GetTesselationVertices(Color.White));
                if (area1 != addArea) throw new NotImplementedException();
                if (area2 != subArea) throw new NotImplementedException();
            }
        }

        private static double GetArea(List<VertexPositionColor> list)
        {
            double area = 0;
            for (int i = 0; i < list.Count / 3; i++)
            {
                Vector3 v1 = list[i * 3 + 2].Position - list[i * 3 + 1].Position;
                Vector3 v2 = list[i * 3 + 1].Position - list[i * 3].Position;
                area += v1.X * v2.Y - v1.Y * v2.X;
            }
            return -area / 2;
        }

        private static SectorConstrainedOSMAreaGraph MakeRect(Dictionary<long, Vector2d> nodes, int x1, int y1, int x2, int y2)
        {
            SectorConstrainedOSMAreaGraph graph = new SectorConstrainedOSMAreaGraph();
            AddLine(nodes, graph, x1, y1, x1, y2); // down
            AddLine(nodes, graph, x1, y2, x2, y2); // right
            AddLine(nodes, graph, x2, y2, x2, y1); // up
            AddLine(nodes, graph, x2, y1, x1, y1); // left
            return graph;
        }

        private static SectorConstrainedOSMAreaGraph MakeOpenRect(Dictionary<long, Vector2d> nodes, int x1, int y1, int x2, int y2, bool leftOpen)
        {
            SectorConstrainedOSMAreaGraph graph = new SectorConstrainedOSMAreaGraph();
            if (!leftOpen) AddLine(nodes, graph, x1, y1, x1, y2); // down
            AddLine(nodes, graph, x1, y2, x2, y2); // right
            if (leftOpen) AddLine(nodes, graph, x2, y2, x2, y1); // up
            AddLine(nodes, graph, x2, y1, x1, y1); // left
            if (leftOpen)
            {
                MarkStartPoint(graph, x1, y2);
                MarkEndPoint(graph, x1, y1);
            }
            else
            {
                MarkStartPoint(graph, x2, y1);
                MarkEndPoint(graph, x2, y2);
            }
            return graph;
        }

        private static void MarkEndPoint(SectorConstrainedOSMAreaGraph graph, int x, int y)
        {
            long n = (x + 500) * 1000 + (y + 500);
            graph.nodes[n].id = -1;
            graph.nodes[n].v = new Vector2d(x, y);
            graph.nodes.Remove(n);
        }

        private static void MarkStartPoint(SectorConstrainedOSMAreaGraph graph, int x, int y)
        {
            long n = (x + 500) * 1000 + (y + 500);
            graph.nodes[n].id = -1;
            graph.nodes[n].v = new Vector2d(x, y);
            graph.startPoints.Add(graph.nodes[n]);
            graph.nodes.Remove(n);
        }

        private static void AddLine(Dictionary<long, Vector2d> nodes, SectorConstrainedOSMAreaGraph graph, int x1, int y1, int x2, int y2)
        {
            if (x1 == x2)
            {
                int length = Math.Abs(y2 - y1);
                int inc = (y2 - y1) / length;
                for (int i = 0; i < length; i++)
                {
                    AddLineSeg(nodes, graph, x1, y1 + i * inc, x2, y1 + (i + 1) * inc);
                }
            }
            else if (y1 == y2)
            {

                int length = Math.Abs(x2 - x1);
                int inc = (x2 - x1) / length;
                for (int i = 0; i < length; i++)
                {
                    AddLineSeg(nodes, graph, x1 + i * inc, y1, x1 + (i + 1) * inc, y2);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void AddLineSeg(Dictionary<long, Vector2d> nodes, SectorConstrainedOSMAreaGraph graph, int x1, int y1, int x2, int y2)
        {
            long n1 = (x1 + 500) * 1000 + (y1 + 500);
            long n2 = (x2 + 500) * 1000 + (y2 + 500);
            if (!graph.nodes.ContainsKey(n1)) graph.nodes.Add(n1, new AreaNode() { id = n1 });
            if (!nodes.ContainsKey(n1)) nodes[n1] = new Vector2d(x1, y1);
            if (!graph.nodes.ContainsKey(n2)) graph.nodes.Add(n2, new AreaNode() { id = n2 });
            if (!nodes.ContainsKey(n2)) nodes[n2] = new Vector2d(x2, y2);
            graph.nodes[n1].next = graph.nodes[n2];
            graph.nodes[n2].prev = graph.nodes[n1];
        }
    }
}

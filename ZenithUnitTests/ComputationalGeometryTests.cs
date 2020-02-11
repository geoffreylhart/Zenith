using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            SectorConstrainedOSMAreaGraph square1 = MakeRect(blobs.nodes, 0, 0, 2, 2);
            SectorConstrainedOSMAreaGraph square2 = MakeRect(blobs.nodes, 2, 0, 3, 1);
            square1.Subtract(square2, blobs);
        }

        private SectorConstrainedOSMAreaGraph MakeRect(Dictionary<long, Vector2d> nodes, int x1, int y1, int x2, int y2)
        {
            SectorConstrainedOSMAreaGraph graph = new SectorConstrainedOSMAreaGraph();
            AddLine(nodes, graph, x1, y1, x1, y2); // down
            AddLine(nodes, graph, x1, y2, x2, y2); // right
            AddLine(nodes, graph, x2, y2, x2, y1); // up
            AddLine(nodes, graph, x2, y1, x1, y1); // left
            return graph;
        }

        private void AddLine(Dictionary<long, Vector2d> nodes, SectorConstrainedOSMAreaGraph graph, int x1, int y1, int x2, int y2)
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

        private void AddLineSeg(Dictionary<long, Vector2d> nodes, SectorConstrainedOSMAreaGraph graph, int x1, int y1, int x2, int y2)
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

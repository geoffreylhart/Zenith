using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    public class SectorConstrainedOSMAreaGraph
    {
        public Dictionary<long, AreaNode> nodes = new Dictionary<long, AreaNode>();
        public HashSet<AreaNode> startPoints = new HashSet<AreaNode>();

        internal SectorConstrainedOSMAreaGraph Add(SectorConstrainedOSMAreaGraph map)
        {
            return this;
        }

        internal SectorConstrainedOSMAreaGraph Subtract(SectorConstrainedOSMAreaGraph map)
        {
            return this;
        }

        internal SectorConstrainedAreaMap Finalize(BlobCollection blobs)
        {
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            SectorConstrainedAreaMap map = new SectorConstrainedAreaMap();
            foreach (var startPoint in startPoints)
            {
                List<Vector2d> newPath = new List<Vector2d>();
                AreaNode curr = startPoint;
                while (true)
                {
                    if (curr.v == null)
                    {
                        newPath.Add(blobs.nodes[curr.id]);
                    }
                    else
                    {
                        newPath.Add(curr.v);
                    }
                    explored.Add(curr);
                    if (curr.next == null) break;
                    curr = curr.next;
                }
                map.paths.Add(newPath);
            }
            foreach (var node in nodes.Values)
            {
                if (explored.Contains(node)) continue;
                // just loops can be found at this point
                List<Vector2d> newLoop = new List<Vector2d>();
                AreaNode curr = node;
                while (true)
                {
                    newLoop.Add(blobs.nodes[curr.id]);
                    explored.Add(curr);
                    if (curr.next == node) break;
                    curr = curr.next;
                }
                newLoop.Add(blobs.nodes[node.id]); // finish off the loop;
                if (ApproximateCW(newLoop))
                {
                    map.inners.Add(newLoop);
                }
                else
                {
                    map.outers.Add(newLoop);
                }
                map.paths.Add(newLoop);
            }
            return map;
        }

        private static bool ApproximateCW(List<Vector2d> loop)
        {
            double area = 0;
            // calculate that area
            Vector2d basePoint = loop.First();
            for (int i = 1; i < loop.Count; i++)
            {
                Vector2d prev = loop[i - 1];
                Vector2d next = loop[i];
                Vector2d line1 = prev - basePoint;
                Vector2d line2 = next - prev;
                area += (line2.X * line1.Y - line2.Y * line1.X) / 2; // random cross-product logic
            }
            bool isCW = area < 0; // based on the coordinate system we're using, with X right and Y down
            return isCW;
        }
    }

    public class AreaNode
    {
        public AreaNode next;
        public AreaNode prev;
        public long id = -1; // -1 when at edge
        public Vector2d v = null; // null when has an id
    }
}

using GeoAPI.Geometries;
using NetTopologySuite.Index.Strtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    public class BlobsIntersector
    {
        internal static void DoIntersections(BlobCollection blobs)
        {
            Dictionary<string, long> uids = new Dictionary<string, long>(); // finally decided I needed something to guarantee uniqueness like this TODO: can probably eliminate this
            long uidCounter = -1000;
            List<Way> ways = TempGetWays(blobs);
            ways.Add(blobs.borderWay);
            List<WayRef> wayRefs = new List<WayRef>();
            STRtree<WayRef> rtree = new STRtree<WayRef>();
            foreach (var way in ways)
            {
                for (int i = 1; i < way.refs.Count; i++)
                {
                    WayRef wayRef = new WayRef() { wayRef = way, nodePos = i - 1 };
                    wayRefs.Add(wayRef);
                    Vector2d pos1 = blobs.nodes[way.refs[i - 1]];
                    Vector2d pos2 = blobs.nodes[way.refs[i]];
                    var env = new Envelope(Math.Min(pos1.X, pos2.X), Math.Max(pos1.X, pos2.X), Math.Min(pos1.Y, pos2.Y), Math.Max(pos1.Y, pos2.Y));
                    rtree.Insert(env, wayRef);
                }
            }
            rtree.Build();
            Dictionary<Way, List<NewIntersection>> intersections = new Dictionary<Way, List<NewIntersection>>();
            foreach (var n1 in wayRefs)
            {
                Vector2d pos1 = blobs.nodes[n1.wayRef.refs[n1.nodePos]];
                Vector2d pos2 = blobs.nodes[n1.wayRef.refs[n1.nodePos + 1]];
                var env = new Envelope(Math.Min(pos1.X, pos2.X), Math.Max(pos1.X, pos2.X), Math.Min(pos1.Y, pos2.Y), Math.Max(pos1.Y, pos2.Y));
                foreach (var n2 in rtree.Query(env))
                {
                    if (n1.GetHashCode() <= n2.GetHashCode()) continue; // as a way of preventing the same query twice
                    long Aid = n1.wayRef.refs[n1.nodePos];
                    long Bid = n1.wayRef.refs[n1.nodePos + 1];
                    long Cid = n2.wayRef.refs[n2.nodePos];
                    long Did = n2.wayRef.refs[n2.nodePos + 1];
                    Vector2d A = blobs.nodes[Aid];
                    Vector2d B = blobs.nodes[Bid];
                    Vector2d C = blobs.nodes[Cid];
                    Vector2d D = blobs.nodes[Did];
                    if (Math.Min(A.X, B.X) > Math.Max(C.X, D.X)) continue;
                    if (Math.Max(A.X, B.X) < Math.Min(C.X, D.X)) continue;
                    if (Math.Min(A.Y, B.Y) > Math.Max(C.Y, D.Y)) continue;
                    if (Math.Max(A.Y, B.Y) < Math.Min(C.Y, D.Y)) continue;
                    string intersectionKey = Aid + "," + Cid;
                    // TODO: we're going to treat -1 as always matching for now
                    bool ACSame = Aid == Cid;
                    bool ADSame = Aid == Did;
                    bool BCSame = Bid == Cid;
                    bool BDSame = Bid == Did;
                    // a subset of possible tiny angles that can cause rounding errors
                    // only thing that changes between these is the condition, line direction, and the newpoint id
                    // TODO: is this really what fixed the nonsense at 240202043? the angleDiff was only 0.009, which seems too big to cause an issue
                    bool someCollinear = false;
                    bool isBorderIntersection = Aid < 0 || Bid < 0 || Cid < 0 || Did < 0;
                    someCollinear |= CheckCollinear(Aid, n2.nodePos, n2.nodePos + 1, n2, intersections, blobs, true, isBorderIntersection);
                    someCollinear |= CheckCollinear(Bid, n2.nodePos, n2.nodePos + 1, n2, intersections, blobs, true, isBorderIntersection);
                    someCollinear |= CheckCollinear(Cid, n1.nodePos, n1.nodePos + 1, n1, intersections, blobs, true, isBorderIntersection);
                    someCollinear |= CheckCollinear(Did, n1.nodePos, n1.nodePos + 1, n1, intersections, blobs, true, isBorderIntersection);
                    if (!ACSame && !ADSame && !BCSame && !BDSame) // proper intersection
                    {
                        if (someCollinear)
                        {
                            if (n1.wayRef.id == n2.wayRef.id)
                            {
                                n1.wayRef.selfIntersects = true; // mark for destruction, probably
                            }
                        }
                        else
                        {
                            // let's sort these lines to guarantee duplicates by value
                            long[] ns = new long[] { Aid, Bid, Cid, Did };
                            if (ns[0] >= ns[2]) ns = ns.Reverse().ToArray();
                            if (ns[0] >= ns[1])
                            {
                                var t = ns[1];
                                ns[1] = ns[0];
                                ns[0] = t;
                            }
                            if (ns[2] >= ns[3])
                            {
                                var t = ns[3];
                                ns[3] = ns[2];
                                ns[2] = t;
                            }
                            Vector2d intersection = Intersect(blobs.nodes[ns[0]], blobs.nodes[ns[1]], blobs.nodes[ns[2]], blobs.nodes[ns[3]]);
                            if (intersection != null)
                            {
                                long intersectionID;
                                if (uids.ContainsKey(intersectionKey))
                                {
                                    intersectionID = uids[intersectionKey];
                                }
                                else
                                {
                                    intersectionID = uidCounter--;
                                    uids[intersectionKey] = intersectionID;
                                }
                                NewIntersection newNode1 = new NewIntersection() { nodeID = intersectionID, wayRef = n1.wayRef, nodePos = n1.nodePos + 1 };
                                NewIntersection newNode2 = new NewIntersection() { nodeID = intersectionID, wayRef = n2.wayRef, nodePos = n2.nodePos + 1 };
                                blobs.nodes[intersectionID] = intersection;
                                if (!intersections.ContainsKey(n1.wayRef)) intersections.Add(n1.wayRef, new List<NewIntersection>());
                                intersections[n1.wayRef].Add(newNode1);
                                if (!intersections.ContainsKey(n2.wayRef)) intersections.Add(n2.wayRef, new List<NewIntersection>());
                                intersections[n2.wayRef].Add(newNode2);
                                if (n1.wayRef.id == n2.wayRef.id)
                                {
                                    n1.wayRef.selfIntersects = true; // mark for destruction, probably
                                }
                            }
                        }
                    }
                }
            }
            List<long> wayids = new List<long>();
            foreach (var pair in intersections)
            {
                if (wayids.Contains(pair.Key.id)) throw new NotImplementedException();
                wayids.Add(pair.Key.id);
            }
            foreach (var pair in intersections)
            {
                foreach (var intersection in pair.Value)
                {
                    intersection.sortRank = Sorter(intersection, blobs);
                }
            }
            foreach (var pair in intersections)
            {
                var sorted = pair.Value.OrderBy(x => x.sortRank).ToList();
                // get rid of duplicates
                for (int i = sorted.Count - 1; i > 0; i--)
                {
                    if (sorted[i].nodeID == sorted[i - 1].nodeID) sorted.RemoveAt(i);
                }
                // now insert them
                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    pair.Key.refs.Insert(sorted[i].nodePos, sorted[i].nodeID);
                }
            }
            RemoveDuplicates(blobs); // remove duplicates at the end to also remove duplicate intersections
        }

        // TODO: somehow merge this with DoIntersections all in one pass elegantly
        // this method is called before all of these maps get added together
        // it will detect and remove shapes found inside each other
        public static void FixLoops(List<SectorConstrainedOSMAreaGraph> addingMaps, BlobCollection blobs)
        {
            // TODO: I think we still need to exclude intersecting loops from our thing
            STRtree<LoopRef> rtree = new STRtree<LoopRef>();
            List<MainLoopRef> mainLoopRefs = new List<MainLoopRef>();
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            foreach (var addingMap in addingMaps)
            {
                foreach (var nodeList in addingMap.nodes.Values)
                {
                    foreach (var node in nodeList)
                    {
                        if (explored.Contains(node)) continue;
                        List<AreaNode> newLoop = new List<AreaNode>();
                        AreaNode curr = node;
                        while (true)
                        {
                            newLoop.Add(curr);
                            explored.Add(curr);
                            if (curr.next == node)
                            {
                                mainLoopRefs.Add(new MainLoopRef() { nodes = newLoop, graph = addingMap });
                                break;
                            }
                            curr = curr.next;
                        }
                    }
                }
            }
            foreach (var loopRef in mainLoopRefs)
            {
                for (int i = 0; i < loopRef.nodes.Count; i++)
                {
                    Vector2d pos1 = blobs.nodes[loopRef.nodes[i].id];
                    Vector2d pos2 = blobs.nodes[loopRef.nodes[(i + 1) % loopRef.nodes.Count].id];
                    var env = new Envelope(Math.Min(pos1.X, pos2.X), Math.Max(pos1.X, pos2.X), Math.Min(pos1.Y, pos2.Y), Math.Max(pos1.Y, pos2.Y));
                    rtree.Insert(env, new LoopRef() { nodes = loopRef.nodes, graph = loopRef.graph, v1 = pos1, v2 = pos2, n1 = loopRef.nodes[i].id, n2 = loopRef.nodes[(i + 1) % loopRef.nodes.Count].id });
                }
            }
            rtree.Build();
            foreach (var loopRef in mainLoopRefs)
            {
                loopRef.isCW = GetArea(loopRef.nodes, blobs) < 0;
            }
            List<MainLoopRef> remove = new List<MainLoopRef>();
            foreach (var loopRef in mainLoopRefs)
            {
                HashSet<long> nodesLookup = new HashSet<long>();
                foreach (var n in loopRef.nodes) nodesLookup.Add(n.id);
                var node = blobs.nodes[loopRef.nodes.First().id];
                Vector2d v1 = new Vector2d(node.X, 10);
                Vector2d v2 = new Vector2d(node.X, -10);
                var env = new Envelope(node.X, node.X, -10, 10);
                var intersections = rtree.Query(env);
                bool doRemove = false;
                foreach (var group in intersections.GroupBy(x => x.graph))
                {
                    if (group.Any(x => x.nodes.Any(y => nodesLookup.Contains(y.id)))) continue; // let's ignore WHOLE GRAPHS that intersect with us (might need to revise this thinking)
                    var lessfiltered = group.Where(x => x.v1.X != x.v2.X).ToList(); // skip vertical intersections
                    var sorted = lessfiltered.Where(x => Math.Min(x.v1.X, x.v2.X) != node.X).ToList(); // on perfect-overlap of adjoining lines, this will count things appropriately
                    sorted = sorted.OrderBy(x => -Intersect(v1, v2, x.v1, x.v2).Y).ToList(); // order from bottom to top
                    List<int> prevSwaps = new List<int>();
                    for (int i = 1; i < sorted.Count; i++)
                    {
                        // swap perfectly adjacent if necessary
                        if ((sorted[i - 1].v2 == sorted[i].v1 && sorted[i - 1].v2.X == node.X && sorted[i - 1].V1Y() < sorted[i].V2Y()) || (sorted[i - 1].v1 == sorted[i].v2 && sorted[i - 1].v1.X == node.X && sorted[i - 1].V2Y() < sorted[i].V1Y()))
                        {
                            var temp = sorted[i - 1];
                            sorted[i - 1] = sorted[i];
                            sorted[i] = temp;
                            prevSwaps.Add(i - 1);
                        }
                    }
                    for (int i = 1; i < sorted.Count; i++)
                    {
                        if (sorted[i - 1].IsLeftToRight() == sorted[i].IsLeftToRight()) throw new NotImplementedException(); // malformed shape
                    }
                    if (sorted.Any(x => x.nodes == loopRef.nodes)) continue; // don't care about our own graph, just validating
                    // remove duplicates that overlap perfectly
                    for (int i = sorted.Count - 1; i > 0; i--)
                    {
                        if (sorted[i].ContainsNode(sorted[i - 1].v1) || sorted[i].ContainsNode(sorted[i - 1].v2))
                        {
                            Vector2d inCommon = sorted[i].ContainsNode(sorted[i - 1].v1) ? sorted[i - 1].v1 : sorted[i - 1].v2;
                            if (inCommon.X == node.X)
                            {
                                sorted.RemoveRange(i - 1, 2);
                                i -= 2;
                            }
                        }
                    }
                    var below = sorted.Where(x => Intersect(v1, v2, x.v1, x.v2).Y > node.Y);
                    var above = sorted.Where(x => Intersect(v1, v2, x.v1, x.v2).Y < node.Y);
                    if (!loopRef.isCW && above.Count() > 0 && !above.First().IsLeftToRight()) doRemove = true;
                    if (loopRef.isCW && below.Count() > 0 && below.Last().IsLeftToRight()) doRemove = true;
                }
                if (doRemove) remove.Add(loopRef);
            }
            // expect 14
            foreach (var r in remove)
            {
                foreach (var node in r.nodes)
                {
                    r.graph.nodes[node.id].Remove(node);
                    if (r.graph.nodes[node.id].Count == 0) r.graph.nodes.Remove(node.id);
                }
            }
        }

        private static double GetArea(List<AreaNode> loop, BlobCollection blobs)
        {
            double area = 0;
            // calculate that area
            Vector2d basePoint = blobs.nodes[loop.First().id];
            for (int i = 0; i < loop.Count; i++)
            {
                long prev = loop[i].id;
                long next = loop[(i + 1) % loop.Count].id;
                Vector2d line1 = blobs.nodes[prev] - basePoint;
                Vector2d line2 = blobs.nodes[next] - blobs.nodes[prev];
                area += (line2.X * line1.Y - line2.Y * line1.X) / 2; // random cross-product logic
            }
            return area;
        }

        public class MainLoopRef
        {
            public List<AreaNode> nodes;
            public bool isCW; // remember, ccw is a solid shape
            public SectorConstrainedOSMAreaGraph graph;
        }

        public class LoopRef
        {
            public long n1;
            public long n2;
            public Vector2d v1;
            public Vector2d v2;
            public List<AreaNode> nodes;
            //public bool isCW; // remember, ccw is a solid shape
            public SectorConstrainedOSMAreaGraph graph;
            public bool IsLeftToRight()
            {
                return v1.X < v2.X;
            }

            internal bool ContainsNode(Vector2d node)
            {
                return v1.Equals(node) || v2.Equals(node);
            }

            internal double V1Y()
            {
                Vector2d diff = v1 - v2;
                return diff.Y / diff.Length();
            }

            internal double V2Y()
            {
                Vector2d diff = v2 - v1;
                return diff.Y / diff.Length();
            }
        }

        private static void RemoveDuplicates(BlobCollection blobs)
        {
            Dictionary<Vector2d, long> nodeHash = new Dictionary<Vector2d, long>();
            List<Way> ways = TempGetWays(blobs);
            ways.Add(blobs.borderWay); // whoops, pretty important
            HashSet<long> dupes = new HashSet<long>();
            foreach (var node in blobs.nodes)
            {
                if (nodeHash.ContainsKey(node.Value))
                {
                    dupes.Add(node.Key);
                }
                else
                {
                    nodeHash.Add(node.Value, node.Key);
                }
            }
            // HOLY CRAP: way 587987916 and 587987919 are identical overlapping shapes, that's why it failed (I thought it was one way with a zero-length edge)
            foreach (var way in ways)
            {
                for (int i = way.refs.Count - 1; i >= 0; i--)
                {
                    if (dupes.Contains(way.refs[i]))
                    {
                        way.refs[i] = nodeHash[blobs.nodes[way.refs[i]]];
                    }
                    // remove consecutive dupes
                    if (i + 1 < way.refs.Count && way.refs[i] == way.refs[i + 1])
                    {
                        way.refs.RemoveAt(i + 1);
                    }
                }
            }
        }

        private static double Sorter(NewIntersection intersection, BlobCollection blobs)
        {
            // return ex: 2.5 means halfway between ref[2] and ref[3]
            Vector2d A = blobs.nodes[intersection.wayRef.refs[intersection.nodePos - 1]];
            Vector2d B = blobs.nodes[intersection.nodeID];
            Vector2d C = blobs.nodes[intersection.wayRef.refs[intersection.nodePos]];
            double partial = (B - A).Length() / (C - A).Length();
            if (partial < -0.1 || partial > 1.1) throw new NotImplementedException();
            return intersection.nodePos - 1 + partial;
        }

        // "temporarily" copy the logic to get all of our ways
        private static List<Way> TempGetWays(BlobCollection blobs)
        {
            Dictionary<long, Way> wayLookup = new Dictionary<long, Way>();
            foreach (var way in blobs.EnumerateWays(false)) wayLookup[way.id] = way;
            HashSet<long> ways = new HashSet<long>();
            HashSet<long> innersOuters = new HashSet<long>();
            HashSet<long> otherInnerOuters = new HashSet<long>();
            foreach (var way in TempGetRelationWays("natural", "water", blobs))
            {
                ways.Add(way);
                innersOuters.Add(way);
            }
            foreach (var way in TempGetRelationWays(blobs))
            {
                if (!innersOuters.Contains(way)) otherInnerOuters.Add(way);
            }
            foreach (var way in TempGetWays("natural", "coastline", blobs)) ways.Add(way);
            foreach (var way in TempGetWays("natural", "water", blobs))
            {
                if (!innersOuters.Contains(way) && wayLookup[way].refs.Count > 2)
                {
                    // some folks forget to close a simple way, or perhaps the mistake is tagging subcomponents of a relation
                    // then there's just straight up errors like way 43291726
                    if (wayLookup[way].refs.Last() != wayLookup[way].refs.First())
                    {
                        if (otherInnerOuters.Contains(way)) continue; // unsure of how else to ignore bad ways like 43815149
                        wayLookup[way].refs.Add(wayLookup[way].refs.First());
                    }
                }
                ways.Add(way);
            }
            return ways.Where(x => wayLookup.ContainsKey(x)).Select(x => wayLookup[x]).ToList();
        }

        private static IEnumerable<long> TempGetRelationWays(string key, string value, BlobCollection blobs)
        {
            HashSet<long> ways = new HashSet<long>();
            foreach (var blob in blobs.blobs)
            {
                if (blob.type != "OSMData") continue;
                int typeIndex = blob.pBlock.stringtable.vals.IndexOf("type");
                int multipolygonIndex = blob.pBlock.stringtable.vals.IndexOf("multipolygon");
                int outerIndex = blob.pBlock.stringtable.vals.IndexOf("outer");
                int innerIndex = blob.pBlock.stringtable.vals.IndexOf("inner");
                int keyIndex = blob.pBlock.stringtable.vals.IndexOf(key);
                int valueIndex = blob.pBlock.stringtable.vals.IndexOf(value);
                if (new[] { typeIndex, multipolygonIndex, outerIndex, innerIndex, keyIndex, valueIndex }.Contains(-1)) continue;
                foreach (var pGroup in blob.pBlock.primitivegroup)
                {
                    foreach (var relation in pGroup.relations)
                    {
                        bool isKeyValue = false;
                        bool isTypeMultipolygon = false;
                        for (int i = 0; i < relation.keys.Count; i++)
                        {
                            if (relation.keys[i] == keyIndex && relation.vals[i] == valueIndex) isKeyValue = true;
                            if (relation.keys[i] == typeIndex && relation.vals[i] == multipolygonIndex) isTypeMultipolygon = true;
                        }
                        if (isKeyValue && isTypeMultipolygon)
                        {
                            for (int i = 0; i < relation.roles_sid.Count; i++)
                            {
                                // just outer for now
                                if (relation.types[i] == 1)
                                {
                                    if (relation.roles_sid[i] == 0 && innerIndex != 0 && outerIndex != 0)
                                    {
                                        // some ways are in a relation without any inner/outer tag
                                        // ex: 359181377 in relation 304768
                                        ways.Add(relation.memids[i]);
                                    }
                                    else
                                    {
                                        if (relation.roles_sid[i] == innerIndex) ways.Add(relation.memids[i]);
                                        if (relation.roles_sid[i] == outerIndex) ways.Add(relation.memids[i]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return ways;
        }

        private static IEnumerable<long> TempGetRelationWays(BlobCollection blobs)
        {
            HashSet<long> ways = new HashSet<long>();
            foreach (var blob in blobs.blobs)
            {
                if (blob.type != "OSMData") continue;
                int typeIndex = blob.pBlock.stringtable.vals.IndexOf("type");
                int multipolygonIndex = blob.pBlock.stringtable.vals.IndexOf("multipolygon");
                int outerIndex = blob.pBlock.stringtable.vals.IndexOf("outer");
                int innerIndex = blob.pBlock.stringtable.vals.IndexOf("inner");
                if (new[] { typeIndex, multipolygonIndex, outerIndex, innerIndex }.Contains(-1)) continue;
                foreach (var pGroup in blob.pBlock.primitivegroup)
                {
                    foreach (var relation in pGroup.relations)
                    {
                        bool isTypeMultipolygon = false;
                        for (int i = 0; i < relation.keys.Count; i++)
                        {
                            if (relation.keys[i] == typeIndex && relation.vals[i] == multipolygonIndex) isTypeMultipolygon = true;
                        }
                        if (isTypeMultipolygon)
                        {
                            List<long> innerWayIds = new List<long>();
                            List<long> outerWayIds = new List<long>();
                            for (int i = 0; i < relation.roles_sid.Count; i++)
                            {
                                // just outer for now
                                if (relation.types[i] == 1)
                                {
                                    ways.Add(relation.memids[i]);
                                }
                            }
                        }
                    }
                }
            }
            return ways;
        }

        private static IEnumerable<long> TempGetWays(string key, string value, BlobCollection blobs)
        {
            return blobs.EnumerateWays(false).Where(x => x.keyValues.ContainsKey(key) && x.keyValues[key] == value).Select(x => x.id);
        }

        // also setup the collinearness
        private static bool CheckCollinear(long v, int aPos, int bPos, WayRef wayRefAB, Dictionary<Way, List<NewIntersection>> intersections, BlobCollection blobs, bool doCollinearness, bool isBorderIntersection)
        {
            bool isCollinear = false;
            if (isBorderIntersection)
            {
                long a = wayRefAB.wayRef.refs[aPos];
                long b = wayRefAB.wayRef.refs[bPos];
                if (v == a || v == b) return false; // points are already shared, so we'll ignore it
                Vector2d aV = blobs.nodes[a];
                Vector2d bV = blobs.nodes[b];
                Vector2d vV = blobs.nodes[v];
                if (vV.X == aV.X && vV.X == bV.X && vV.Y < 1 && vV.Y > 0) isCollinear = true;
                if (vV.Y == aV.Y && vV.Y == bV.Y && vV.X < 1 && vV.X > 0) isCollinear = true;
            }
            else
            {
                long a = wayRefAB.wayRef.refs[aPos];
                long b = wayRefAB.wayRef.refs[bPos];
                if (v == a || v == b) return false; // points are already shared, so we'll ignore it
                double angle1 = CalcAngleDiff(a, b, a, v, blobs);
                double angle2 = CalcAngleDiff(b, a, b, v, blobs);
                double angleDiff = 0.01;
                if (angle1 < angleDiff && angle2 < angleDiff)
                {
                    isCollinear = true;
                }
            }
            if (isCollinear)
            {
                if (doCollinearness)
                {
                    NewIntersection newNode = new NewIntersection() { wayRef = wayRefAB.wayRef, nodeID = v, nodePos = aPos + 1 };
                    if (!intersections.ContainsKey(wayRefAB.wayRef)) intersections.Add(wayRefAB.wayRef, new List<NewIntersection>());
                    intersections[wayRefAB.wayRef].Add(newNode);
                }
                return true;
            }
            return false;
        }

        private static double CalcAngleDiff(long A, long B, long C, long D, BlobCollection blobs)
        {
            Vector2d line1 = blobs.nodes[B] - blobs.nodes[A];
            Vector2d line2 = blobs.nodes[D] - blobs.nodes[C];
            double angleDiff = Math.Atan2(line1.Y, line1.X) - Math.Atan2(line2.Y, line2.X);
            angleDiff = (angleDiff + 2 * Math.PI) % (2 * Math.PI);
            if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;
            return angleDiff;
        }

        public class WayRef
        {
            public Way wayRef;
            public int nodePos;
        }

        public class NewIntersection
        {
            public Way wayRef;
            public int nodePos;
            public long nodeID;
            public double sortRank;
        }

        public static Vector2d Intersect(Vector2d a, Vector2d b, Vector2d c, Vector2d d)
        {
            if ((a.X == c.X && a.Y == c.Y) || (a.X == d.X && a.Y == d.Y) || (b.X == c.X && b.Y == c.Y) || (b.X == d.X && b.Y == d.Y)) return null; // TODO: sometimes consecutive nodes have duplicate coordinates, let's ignore that for now (alternatively, merge those nodes)
            // copied from wiki, sure
            double t = ((a.X - c.X) * (c.Y - d.Y) - (a.Y - c.Y) * (c.X - d.X)) / ((a.X - b.X) * (c.Y - d.Y) - (a.Y - b.Y) * (c.X - d.X));
            double u = -((a.X - b.X) * (a.Y - c.Y) - (a.Y - b.Y) * (a.X - c.X)) / ((a.X - b.X) * (c.Y - d.Y) - (a.Y - b.Y) * (c.X - d.X));
            if (double.IsNaN(t) || double.IsNaN(u)) return null;
            if (t < 0 || t > 1 || u < 0 || u > 1) return null;
            Vector2d answer = a + (b - a) * t;
            // deal with perfectly horizontal/vertical lines (aka border intersections)
            if (a.X == b.X) answer.X = a.X;
            if (c.X == d.X) answer.X = c.X;
            if (a.Y == b.Y) answer.Y = a.Y;
            if (c.Y == d.Y) answer.Y = c.Y;
            return answer;
        }
    }
}

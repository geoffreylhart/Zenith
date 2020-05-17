using GeoAPI.Geometries;
using NetTopologySuite.Index.Strtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    class BlobsIntersector
    {
        internal static void DoIntersections(BlobCollection blobs)
        {
            RemoveDuplicates(blobs);
            Dictionary<string, long> uids = new Dictionary<string, long>(); // finally decided I needed something to guarantee uniqueness like this TODO: can probably eliminate this
            long uidCounter = -1000;
            List<Way> ways = TempGetWays(blobs);
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
                    someCollinear |= CheckCollinear(Aid, n2.nodePos, n2.nodePos + 1, n2, intersections, blobs, true);
                    someCollinear |= CheckCollinear(Bid, n2.nodePos, n2.nodePos + 1, n2, intersections, blobs, true);
                    someCollinear |= CheckCollinear(Cid, n1.nodePos, n1.nodePos + 1, n1, intersections, blobs, true);
                    someCollinear |= CheckCollinear(Did, n1.nodePos, n1.nodePos + 1, n1, intersections, blobs, true);
                    if (!ACSame && !ADSame && !BCSame && !BDSame && !someCollinear) // proper intersection
                    {
                        Vector2d intersection = Intersect(A, B, C, D);
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
            List<long> wayids = new List<long>();
            foreach (var pair in intersections)
            {
                if (wayids.Contains(pair.Key.id)) throw new NotImplementedException();
                wayids.Add(pair.Key.id);
            }
            foreach (var pair in intersections)
            {
                var sorted = pair.Value.OrderBy(x => Sorter(x, blobs)).ToList();
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
        }

        private static void RemoveDuplicates(BlobCollection blobs)
        {
            Dictionary<Vector2d, long> nodeHash = new Dictionary<Vector2d, long>();
            List<Way> ways = TempGetWays(blobs);
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
            return intersection.nodePos - 1 + (B - A).Length() / (C - A).Length();
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
                            List<long> innerWayIds = new List<long>();
                            List<long> outerWayIds = new List<long>();
                            for (int i = 0; i < relation.roles_sid.Count; i++)
                            {
                                // just outer for now
                                if (relation.types[i] == 1)
                                {
                                    if (relation.roles_sid[i] == innerIndex) ways.Add(relation.memids[i]);
                                    if (relation.roles_sid[i] == outerIndex) ways.Add(relation.memids[i]);
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
        private static bool CheckCollinear(long v, int aPos, int bPos, WayRef wayRefAB, Dictionary<Way, List<NewIntersection>> intersections, BlobCollection blobs, bool doCollinearness)
        {
            long a = wayRefAB.wayRef.refs[aPos];
            long b = wayRefAB.wayRef.refs[bPos];
            if (v == a || v == b) return false; // points are already shared, so we'll ignore it
            double angle1 = CalcAngleDiff(a, b, a, v, blobs);
            double angle2 = CalcAngleDiff(b, a, b, v, blobs);
            if (angle1 < 0.01 && angle2 < 0.01)
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
        }

        private static Vector2d Intersect(Vector2d a, Vector2d b, Vector2d c, Vector2d d)
        {
            if ((a.X == c.X && a.Y == c.Y) || (a.X == d.X && a.Y == d.Y) || (b.X == c.X && b.Y == c.Y) || (b.X == d.X && b.Y == d.Y)) return null; // TODO: sometimes consecutive nodes have duplicate coordinates, let's ignore that for now (alternatively, merge those nodes)
            // copied from wiki, sure
            double t = ((a.X - c.X) * (c.Y - d.Y) - (a.Y - c.Y) * (c.X - d.X)) / ((a.X - b.X) * (c.Y - d.Y) - (a.Y - b.Y) * (c.X - d.X));
            double u = -((a.X - b.X) * (a.Y - c.Y) - (a.Y - b.Y) * (a.X - c.X)) / ((a.X - b.X) * (c.Y - d.Y) - (a.Y - b.Y) * (c.X - d.X));
            if (double.IsNaN(t) || double.IsNaN(u)) return null;
            if (t < 0 || t > 1 || u < 0 || u > 1) return null;
            return a + (b - a) * t;
        }
    }
}

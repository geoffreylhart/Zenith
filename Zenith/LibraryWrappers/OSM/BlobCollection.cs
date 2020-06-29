using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Zenith.ZGeom;
using Zenith.ZMath;
using static Zenith.LibraryWrappers.OSM.Blob;
using static Zenith.ZGeom.LineGraph;

namespace Zenith.LibraryWrappers.OSM
{
    // responsible for lots of stuff
    public class BlobCollection
    {
        public static double SMALLEST_ALLOWED_AREA = 1E-19; // ex: way 43624681 was -1.6543612251060553E-24, way 608212702 was 1.1312315262122068E-20
        public Dictionary<long, Vector2d> nodes = new Dictionary<long, Vector2d>();
        public List<Blob> blobs;
        private ISector sector;
        private OSMMetaFinal.GridPointInfo gridPointInfo;
        public Way borderWay;

        public BlobCollection(List<Blob> blobs, ISector sector)
        {
            borderWay = new Way();
            borderWay.id = -10;
            borderWay.refs = new List<long>() { -900, -901, -902, -903, -900 }; // arbitrary for now, but ccw for land
            nodes.Add(-900, new Vector2d(0, 0));
            nodes.Add(-901, new Vector2d(0, 1));
            nodes.Add(-902, new Vector2d(1, 1));
            nodes.Add(-903, new Vector2d(1, 0));

            this.blobs = blobs;
            this.sector = sector;
            this.gridPointInfo = OSMMetaFinal.GetGridPointInfo(sector);
            // initialize
            ISector rootSector = sector.GetRoot();
            foreach (var blob in blobs)
            {
                if (blob.type != "OSMData") continue;
                // build node data
                for (int i = 0; i < blob.pBlock.primitivegroup.Count; i++)
                {
                    var pGroup = blob.pBlock.primitivegroup[i];
                    for (int j = 0; j < pGroup.dense.Count; j++)
                    {
                        var d = pGroup.dense[j];
                        for (int k = 0; k < d.id.Count; k++)
                        {
                            double longitude = .000000001 * (blob.pBlock.lon_offset + (blob.pBlock.granularity * d.lon[k]));
                            double latitude = .000000001 * (blob.pBlock.lat_offset + (blob.pBlock.granularity * d.lat[k]));
                            nodes[d.id[k]] = sector.ProjectToLocalCoordinates(new LongLat(longitude * Math.PI / 180, latitude * Math.PI / 180).ToSphereVector());
                        }
                    }
                }
            }
        }

        public BlobCollection()
        {
        }

        internal SectorConstrainedOSMAreaGraph GetAreaMap(string key, string value)
        {
            var simpleWays = EnumerateWays().Where(x => x.keyValues.ContainsKey(key) && x.keyValues[key] == value); // we expect all of these to be closed loops
            // copy this dang multipolygon way id gathering logic
            Dictionary<long, Way> wayLookup = new Dictionary<long, Way>();
            foreach (var way in EnumerateWays()) wayLookup[way.id] = way;
            List<List<long>> inners = new List<List<long>>();
            List<List<long>> outers = new List<List<long>>();
            List<long> relationIds = new List<long>();
            HashSet<long> otherInnerOuters = new HashSet<long>();
            foreach (var blob in blobs)
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
                        if (isTypeMultipolygon)
                        {
                            if (isKeyValue)
                            {
                                List<long> innerWayIds = new List<long>();
                                List<long> outerWayIds = new List<long>();
                                for (int i = 0; i < relation.roles_sid.Count; i++)
                                {
                                    // just outer for now
                                    if (relation.types[i] == 1)
                                    {
                                        if (relation.roles_sid[i] == 0 && innerIndex != 0 && outerIndex != 0)
                                        {
                                            // some ways are in a relation without any inner/outer tag
                                            // ex: 359181377 in relation 304768
                                            outerWayIds.Add(relation.memids[i]);
                                        }
                                        else
                                        {
                                            if (relation.roles_sid[i] == innerIndex) innerWayIds.Add(relation.memids[i]);
                                            if (relation.roles_sid[i] == outerIndex) outerWayIds.Add(relation.memids[i]);
                                        }
                                    }
                                }
                                inners.Add(innerWayIds);
                                outers.Add(outerWayIds);
                                relationIds.Add(relation.id);
                            }
                            else
                            {
                                for (int i = 0; i < relation.roles_sid.Count; i++)
                                {
                                    // just outer for now
                                    if (relation.types[i] == 1)
                                    {
                                        otherInnerOuters.Add(relation.memids[i]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            HashSet<long> innersAndOuters = new HashSet<long>();
            foreach (var innerList in inners)
            {
                foreach (var inner in innerList) innersAndOuters.Add(inner);
            }
            foreach (var outerList in outers)
            {
                foreach (var outer in outerList) innersAndOuters.Add(outer);
            }
            // end gathering logic
            // add each simple way, flipping them where necessary
            List<SectorConstrainedOSMAreaGraph> addingMaps = new List<SectorConstrainedOSMAreaGraph>();
            foreach (var way in simpleWays)
            {
                if (innersAndOuters.Contains(way.id)) continue; // sometimes lake multipolygons also tag several pieces - at best this is redundant, and at worst causes errors
                if (way.refs.Count < 3) continue; // we -usually- only ever see lines with multipolygons, but I found a weird way like 43435045
                if (way.selfIntersects) continue; // just ignore ways like 43410874 to keep you sane
                SectorConstrainedOSMAreaGraph simpleMap = new SectorConstrainedOSMAreaGraph();
                var superLoop = new List<Way>() { way };
                if (way.refs.Last() != way.refs.First())
                {
                    if (otherInnerOuters.Contains(way.id)) continue; // unsure of how else to ignore bad ways like 43815149
                    way.refs.Add(way.refs.First()); // some folks forget to close a simple way, or perhaps the mistake is tagging subcomponents of a relation
                }
                if (!IsValid(superLoop)) continue; // ignore relations like 512080985 which contain duplicate nodes
                double wayArea = GetArea(superLoop);
                if (Math.Abs(wayArea) < SMALLEST_ALLOWED_AREA) continue; // ignore zero-area ways since it really messes with the tesselator (ex: way 43624681) TODO: maybe check for absolute zero via node duplication?
                bool isCW = wayArea < 0;
                if (isCW) way.refs.Reverse(); // the simple polygons are always "outers"
                bool untouchedLoop = CheckIfUntouchedAndSpin(superLoop);
                if (untouchedLoop)
                {
                    AddUntouchedLoop(simpleMap, superLoop);
                }
                else
                {
                    AddConstrainedPaths(simpleMap, superLoop);
                    simpleMap.CloseLines(this);
                    if (Constants.DEBUG_MODE) simpleMap.CheckValid();
                }
                simpleMap.RemoveDuplicateLines();
                if (Constants.DEBUG_MODE) simpleMap.CheckValid();
                addingMaps.Add(simpleMap);
            }
            // construct each multipolygon to add separately
            for (int i = 0; i < inners.Count; i++) // foreach multipolygon, basically
            {
                if (OSMMetaFinal.GLOBAL_FINAL.badRelations.Contains(relationIds[i])) continue;
                // TODO: with islands inside of ponds inside of islands inside of ponds, etc. we wouldn't expect this to work
                // however, we're taking advantage of the fact that Add/Subtract doesn't check for that for now (until Finalize)
                SuperWayCollection superInnerWays = GenerateSuperWayCollection(inners[i].Where(x => wayLookup.ContainsKey(x)).Select(x => Copy(wayLookup[x])), true);
                SuperWayCollection superOuterWays = GenerateSuperWayCollection(outers[i].Where(x => wayLookup.ContainsKey(x)).Select(x => Copy(wayLookup[x])), true);
                superInnerWays.loopedWays = superInnerWays.loopedWays.Where(x => Math.Abs(GetArea(x)) >= SMALLEST_ALLOWED_AREA).ToList(); // ignore zero-area ways since it really messes with the tesselator (ex: way 43624681) TODO: maybe check for absolute zero via node duplication?
                superOuterWays.loopedWays = superOuterWays.loopedWays.Where(x => Math.Abs(GetArea(x)) >= SMALLEST_ALLOWED_AREA).ToList(); // ignore zero-area ways since it really messes with the tesselator (ex: way 43624681) TODO: maybe check for absolute zero via node duplication?
                if (!IsValid(superInnerWays)) continue;
                if (!IsValid(superOuterWays)) continue;
                OrientSuperWays(superInnerWays, superOuterWays, gridPointInfo.relations.Contains(relationIds[i]));
                SectorConstrainedOSMAreaGraph innerMap = DoMultipolygon(superInnerWays);
                SectorConstrainedOSMAreaGraph outerMap = DoMultipolygon(superOuterWays);
                if (Constants.DEBUG_MODE) innerMap.CheckValid();
                if (Constants.DEBUG_MODE) outerMap.CheckValid();
                SectorConstrainedOSMAreaGraph multiPolygon = outerMap.Subtract(innerMap, this);
                if (Constants.DEBUG_MODE) multiPolygon.CheckValid();
                addingMaps.Add(multiPolygon);
            }
            SectorConstrainedOSMAreaGraph map = new SectorConstrainedOSMAreaGraph();
            BlobsIntersector.FixLoops(addingMaps, this);
            foreach (var addingMap in addingMaps) map.Add(addingMap, this);
            if (Constants.DEBUG_MODE) map.CheckValid();
            return map;
        }

        private class SuperWayIntersection
        {
            public Vector2d intersection;
            public List<Way> superWay;
            public bool leaving;
            public bool inner;
        }

        private void OrientSuperWays(SuperWayCollection superInnerWays, SuperWayCollection superOuterWays, bool topLeftIsInside)
        {
            List<List<Way>> allLinkedWays = new List<List<Way>>();
            allLinkedWays.AddRange(superInnerWays.linkedWays);
            allLinkedWays.AddRange(superOuterWays.linkedWays);
            List<List<Way>> allLoopedWays = new List<List<Way>>();
            allLoopedWays.AddRange(superInnerWays.loopedWays);
            allLoopedWays.AddRange(superOuterWays.loopedWays);
            // assume all loops dont touch the edge for now (they can be corrected later)
            foreach (var superLoop in allLoopedWays)
            {
                SectorConstrainedOSMAreaGraph temp = new SectorConstrainedOSMAreaGraph();
                bool isCW = ApproximateCW(superLoop);
                if (isCW) // TODO: why did I have this has the opposite logic before??
                {
                    // force to be an "outer"
                    superLoop.Reverse();
                    foreach (var way in superLoop) way.refs.Reverse();
                }
            }

            // now do the -real- orienting
            List<SuperWayIntersection> intersections = new List<SuperWayIntersection>();
            foreach (var superWay in superOuterWays.linkedWays) // we expect these to always start and end outside the sector
            {
                AddIntersections(intersections, superWay, false);
            }
            foreach (var superLoop in superOuterWays.loopedWays)
            {
                bool untouchedLoop = CheckIfUntouchedAndSpin(superLoop);
                if (!untouchedLoop)
                {
                    AddIntersections(intersections, superLoop, false);
                }
            }
            foreach (var superWay in superInnerWays.linkedWays) // we expect these to always start and end outside the sector
            {
                AddIntersections(intersections, superWay, true);
            }
            foreach (var superLoop in superInnerWays.loopedWays)
            {
                bool untouchedLoop = CheckIfUntouchedAndSpin(superLoop);
                if (!untouchedLoop)
                {
                    AddIntersections(intersections, superLoop, true);
                }
            }
            if (intersections.Count % 2 != 0) throw new NotImplementedException();
            intersections = intersections.OrderBy(x => (Math.Atan2(x.intersection.Y - 0.5, x.intersection.X - 0.5) + 8 * Math.PI - (-Math.PI * 3 / 4)) % (2 * Math.PI)).ToList(); // clockwise order starting at top-left
            // look for duplicates that are near each other, had this issue with relation 534928, if they're close enough we'll just ignore them for now - TODO: this may cause issues later
            HashSet<int> ignore = new HashSet<int>();
            for (int i = 0; i < intersections.Count; i++)
            {
                int j = (i + 1) % intersections.Count;
                if ((intersections[i].intersection - intersections[j].intersection).Length() < 0.0000001)
                {
                    ignore.Add(i);
                    ignore.Add(j);
                }
            }
            HashSet<List<Way>> correctDirectionHash = new HashSet<List<Way>>();
            HashSet<List<Way>> incorrectDirectionHash = new HashSet<List<Way>>();
            for (int i = 0; i < intersections.Count; i++)
            {
                if (ignore.Contains(i)) continue;
                bool correctDirection = intersections[i].leaving == (topLeftIsInside ^ (i % 2 == 1));
                if (intersections[i].inner) correctDirection = !correctDirection; // flip all of the inners back, since we're about to subtract them
                if (correctDirection)
                {
                    correctDirectionHash.Add(intersections[i].superWay);
                    if (incorrectDirectionHash.Contains(intersections[i].superWay)) throw new NotImplementedException(); // disagreement
                }
                else
                {
                    incorrectDirectionHash.Add(intersections[i].superWay);
                    if (correctDirectionHash.Contains(intersections[i].superWay)) throw new NotImplementedException(); // disagreement
                }
            }
            foreach (var superWay in incorrectDirectionHash)
            {
                superWay.Reverse();
                foreach (var way in superWay) way.refs.Reverse();
            }
        }

        private void AddIntersections(List<SuperWayIntersection> list, List<Way> superWay, bool inner)
        {
            bool prevIsInside = false;
            if (sector.ContainsCoord(nodes[superWay.First().refs.First()])) throw new NotImplementedException();
            if (sector.ContainsCoord(nodes[superWay.Last().refs.Last()])) throw new NotImplementedException();
            for (int i = 0; i < superWay.Count; i++)
            {
                for (int j = 1; j < superWay[i].refs.Count; j++)
                {
                    long prev = superWay[i].refs[j - 1];
                    long next = superWay[i].refs[j];
                    if (sector.BorderContainsCoord(nodes[next]))
                    {
                        if (prevIsInside) // close-out a line
                        {
                            list.Add(new SuperWayIntersection() { intersection = nodes[next], leaving = true, superWay = superWay, inner = inner });
                        }
                        else // start up a new line
                        {
                            list.Add(new SuperWayIntersection() { intersection = nodes[next], leaving = false, superWay = superWay, inner = inner });
                        }
                        prevIsInside = !prevIsInside;
                    }
                }
            }
        }

        // we'll just apply this to multipolygons for now, skipping relations like 313091
        private bool IsValid(SuperWayCollection wayCollection)
        {
            foreach (var superWay in wayCollection.linkedWays)
            {
                if (!IsValid(superWay)) return false;
                if (sector.ContainsCoord(nodes[superWay.First().refs.First()])) return false;
                if (sector.ContainsCoord(nodes[superWay.Last().refs.Last()])) return false;
            }
            foreach (var superLoop in wayCollection.loopedWays)
            {
                if (!IsValid(superLoop)) return false;
            }
            return true;
        }

        // or loop
        private bool IsValid(List<Way> superWay)
        {
            Dictionary<long, int> lastIndexOf = new Dictionary<long, int>();
            var broken = BreakDownSuperLoop(superWay);
            for (int i = 0; i < broken.Count; i++)
            {
                if (lastIndexOf.ContainsKey(broken[i]) && (lastIndexOf[broken[i]] != 0 || i != broken.Count - 1))
                {
                    return false;
                }
                lastIndexOf[broken[i]] = i;
            }
            return true;
        }

        // TODO: get rid of this dupe logic
        private Way Copy(Way way)
        {
            Way newway = new Way();
            newway.id = way.id;
            newway.refs = new List<long>();
            newway.refs.AddRange(way.refs);
            newway.info = way.info;
            newway.keys = way.keys;
            newway.keyValues = way.keyValues;
            newway.vals = way.vals;
            newway.selfIntersects = way.selfIntersects;
            return newway;
        }

        private SectorConstrainedOSMAreaGraph DoMultipolygon(SuperWayCollection superWays)
        {
            SectorConstrainedOSMAreaGraph map = new SectorConstrainedOSMAreaGraph();
            foreach (var superWay in superWays.linkedWays) // we expect these to always start and end outside the sector
            {
                AddConstrainedPaths(map, superWay);
            }
            map.CloseLines(this);
            map.RemoveDuplicateLines();
            if (Constants.DEBUG_MODE) map.CheckValid();
            foreach (var superLoop in superWays.loopedWays)
            {
                double wayArea = GetArea(superLoop);
                if (Math.Abs(wayArea) < SMALLEST_ALLOWED_AREA) continue; // ignore zero-area ways since it really messes with the tesselator (ex: way 43624681) TODO: maybe check for absolute zero via node duplication?
                bool isCW = wayArea < 0;
                var temp = new SectorConstrainedOSMAreaGraph();
                bool untouchedLoop = CheckIfUntouchedAndSpin(superLoop);
                if (untouchedLoop)
                {
                    AddUntouchedLoop(temp, superLoop);
                }
                else
                {
                    AddConstrainedPaths(temp, superLoop);
                }
                temp.CloseLines(this);
                temp.RemoveDuplicateLines();
                if (Constants.DEBUG_MODE) temp.CheckValid();
                map.Add(temp, this);
            }
            return map;
        }

        private void AddUntouchedLoop(SectorConstrainedOSMAreaGraph map, List<Way> superLoop)
        {
            List<AreaNode> newNodes = new List<AreaNode>();
            var broken = BreakDownSuperLoop(superLoop);
            for (int i = 0; i < broken.Count - 1; i++) // since our loops end in a duplicate
            {
                AreaNode curr = new AreaNode() { id = broken[i] };
                newNodes.Add(curr);
                if (!map.nodes.ContainsKey(broken[i])) map.nodes[broken[i]] = new List<AreaNode>();
                map.nodes[broken[i]].Add(curr);
            }
            for (int i = 0; i < newNodes.Count; i++)
            {
                AreaNode prev = newNodes[i];
                AreaNode next = newNodes[(i + 1) % newNodes.Count];
                prev.next = next;
                next.prev = prev;
            }
        }

        internal SectorConstrainedOSMAreaGraph GetCoastAreaMap(string key, string value)
        {
            // remember: "If you regard this as tracing around an area of land, then the coastline way should be running counterclockwise."
            // gather ways with matching starts/ends to form a super-way, coast ways should always run the same direction, so this becomes easier
            SuperWayCollection superWays = GenerateSuperWayCollection(EnumerateWays().Where(x => x.keyValues.ContainsKey(key) && x.keyValues[key] == value), false);
            SectorConstrainedOSMAreaGraph map = DoMultipolygon(superWays);
            BlobsIntersector.FixLoops(new List<SectorConstrainedOSMAreaGraph>() { map }, this);
            if (map.nodes.Count == 0 && (OSMMetaFinal.IsPixelLand(sector) || borderWay.refs.Count > 5)) // just return a big ol' square
            {
                for (int i = 1; i < borderWay.refs.Count; i++)
                {
                    if (!map.nodes.ContainsKey(borderWay.refs[i]))
                    {
                        map.nodes[borderWay.refs[i]] = new List<AreaNode>();
                        map.nodes[borderWay.refs[i]].Add(new AreaNode() { id = borderWay.refs[i] });
                    }
                }
                for (int i = 1; i < borderWay.refs.Count; i++)
                {
                    AreaNode prev = map.nodes[borderWay.refs[i - 1]].Single();
                    AreaNode next = map.nodes[borderWay.refs[i]].Single();
                    if (prev.id != next.id)
                    {
                        prev.next = next;
                        next.prev = prev;
                    }
                }
            }
            if (Constants.DEBUG_MODE) map.CheckValid();
            return map;
        }

        private void AddConstrainedPaths(SectorConstrainedOSMAreaGraph map, List<Way> superWay)
        {
            if (sector.ContainsCoord(nodes[superWay.First().refs.First()])) throw new NotImplementedException();
            if (sector.ContainsCoord(nodes[superWay.Last().refs.Last()])) throw new NotImplementedException();
            List<AreaNode> nodesToAdd = new List<AreaNode>();
            for (int i = 0; i < superWay.Count; i++)
            {
                for (int j = 1; j < superWay[i].refs.Count; j++)
                {
                    long next = superWay[i].refs[j];
                    nodesToAdd.Add(new AreaNode() { id = next });
                }
            }
            for (int i = 0; i < nodesToAdd.Count; i++)
            {
                var prev = nodesToAdd[(i + nodesToAdd.Count - 1) % nodesToAdd.Count];
                var curr = nodesToAdd[i];
                var next = nodesToAdd[(i + 1) % nodesToAdd.Count];
                if (sector.BorderContainsCoord(nodes[curr.id]))
                {
                    bool add = false;
                    if (!sector.BorderContainsCoord(nodes[next.id]) && sector.ContainsCoord(nodes[next.id]))
                    {
                        curr.next = next;
                        next.prev = curr;
                        add = true;
                    }
                    if (!sector.BorderContainsCoord(nodes[prev.id]) && sector.ContainsCoord(nodes[prev.id]))
                    {
                        curr.prev = prev;
                        prev.next = curr;
                        add = true;
                    }
                    if (sector.BorderContainsCoord(nodes[prev.id]) && nodes[prev.id].X != nodes[curr.id].X && nodes[prev.id].Y != nodes[curr.id].Y)
                    {
                        prev.next = curr;
                        curr.prev = prev;
                        add = true;
                        if (!map.nodes.ContainsKey(prev.id)) map.nodes[prev.id] = new List<AreaNode>();
                        map.nodes[prev.id].Add(prev);
                    }
                    if (add)
                    {
                        if (!map.nodes.ContainsKey(curr.id)) map.nodes[curr.id] = new List<AreaNode>();
                        map.nodes[curr.id].Add(curr);
                    }
                }
                else if (sector.ContainsCoord(nodes[curr.id]))
                {
                    if (!map.nodes.ContainsKey(curr.id)) map.nodes[curr.id] = new List<AreaNode>();
                    map.nodes[curr.id].Add(curr);
                    if (!sector.ContainsCoord(nodes[prev.id])) throw new NotImplementedException(); // previous should be border point or inside
                    if (!sector.ContainsCoord(nodes[next.id])) throw new NotImplementedException(); // next should be border point or inside
                    if (!sector.BorderContainsCoord(nodes[prev.id])) curr.prev = prev; // edges will handle their prev/next logic
                    if (!sector.BorderContainsCoord(nodes[next.id])) curr.next = next;
                }
            }
        }

        public class SuperWayCollection
        {
            public List<List<Way>> linkedWays = null; // gather ways with matching starts/ends to form a super-way, unlike with the coast, multipolygons say outright if a way is inside or outside
            public List<List<Way>> loopedWays = new List<List<Way>>(); // fully closed loops
        }

        // TODO: temporary states are passed around with incorrect metadata on the ways at times (ref reversing and fake ways), ignore this for now since the final product lacks these issues
        private SuperWayCollection GenerateSuperWayCollection(IEnumerable<Way> ways, bool ignoreDirection)
        {
            SuperWayCollection collection = new SuperWayCollection();
            Dictionary<long, List<Way>> startsWith = new Dictionary<long, List<Way>>();
            Dictionary<long, List<Way>> endsWith = new Dictionary<long, List<Way>>();
            // first, we construct our linkedWays and loopedWays
            foreach (var ourWay in ways)
            {
                long ourFirstNode = ourWay.refs.First();
                long ourLastNode = ourWay.refs.Last();
                if (ignoreDirection)
                {
                    // force existing superWays to align with ourWay
                    if (endsWith.ContainsKey(ourLastNode) && startsWith.ContainsKey(ourFirstNode) && endsWith[ourLastNode] == startsWith[ourFirstNode]) // actually very common
                    {
                        JustFlipIt(endsWith[ourLastNode], startsWith, endsWith);
                    }
                    else
                    {
                        if (endsWith.ContainsKey(ourLastNode))
                        {
                            JustFlipIt(endsWith[ourLastNode], startsWith, endsWith);
                        }
                        if (startsWith.ContainsKey(ourFirstNode))
                        {
                            JustFlipIt(startsWith[ourFirstNode], startsWith, endsWith);
                        }
                    }
                }
                if (endsWith.ContainsKey(ourFirstNode) && startsWith.ContainsKey(ourLastNode)) // first, try to insert between A & B
                {
                    var lineA = endsWith[ourFirstNode];
                    var lineB = startsWith[ourLastNode];
                    var lineBLastNode = lineB.Last().refs.Last();
                    if (lineA == lineB) // we've got a closed loop here
                    {
                        lineA.Add(ourWay);
                        collection.loopedWays.Add(lineA);
                        endsWith.Remove(ourFirstNode);
                        startsWith.Remove(ourLastNode);
                    }
                    else
                    {
                        lineA.Add(ourWay);
                        lineA.AddRange(lineB);
                        endsWith[lineBLastNode] = lineA;
                        endsWith.Remove(ourFirstNode);
                        startsWith.Remove(ourLastNode);
                    }
                }
                else if (endsWith.ContainsKey(ourFirstNode)) // now, try to append it to something
                {
                    var lineA = endsWith[ourFirstNode];
                    lineA.Add(ourWay);
                    endsWith[ourLastNode] = lineA;
                    endsWith.Remove(ourFirstNode);
                }
                else if (startsWith.ContainsKey(ourLastNode)) // now, try to prepend it to something
                {
                    var lineB = startsWith[ourLastNode];
                    lineB.Insert(0, ourWay);
                    startsWith[ourFirstNode] = lineB;
                    startsWith.Remove(ourLastNode);
                }
                else // it's completely new and disconnected
                {
                    List<Way> us = new List<Way>() { ourWay };
                    if (ourFirstNode == ourLastNode)
                    {
                        collection.loopedWays.Add(us);
                    }
                    else
                    {
                        startsWith[ourFirstNode] = us;
                        endsWith[ourLastNode] = us;
                    }
                }
            }
            collection.linkedWays = startsWith.Values.ToList();
            return collection;
        }

        private void JustFlipIt(List<Way> superWay, Dictionary<long, List<Way>> startsWith, Dictionary<long, List<Way>> endsWith)
        {
            long initialEnd = superWay.Last().refs.Last();
            long initialStart = superWay.First().refs.First();
            superWay.Reverse();
            foreach (var way in superWay) way.refs.Reverse(); // TODO: if this turns out to be expensive, we can optimize this later
            startsWith.Remove(initialStart);
            endsWith.Remove(initialEnd);
            startsWith[initialEnd] = superWay;
            endsWith[initialStart] = superWay;
        }

        private bool ApproximateCW(List<Way> superLoop)
        {
            return GetArea(superLoop) < 0; // based on the coordinate system we're using, with X right and Y down
        }

        private double GetArea(List<Way> superLoop)
        {
            double area = 0;
            // calculate that area
            Vector2d basePoint = nodes[superLoop.First().refs[0]];
            for (int i = 0; i < superLoop.Count; i++)
            {
                for (int j = 1; j < superLoop[i].refs.Count; j++)
                {
                    long prev = superLoop[i].refs[j - 1];
                    long next = superLoop[i].refs[j];
                    Vector2d line1 = nodes[prev] - basePoint;
                    Vector2d line2 = nodes[next] - nodes[prev];
                    area += (line2.X * line1.Y - line2.Y * line1.X) / 2; // random cross-product logic
                }
            }
            return area;
        }

        // this method takes a loop and checks if it ever goes outside the bounds of the sector, then makes sure that the first node is always outside to help other functions deal
        private bool CheckIfUntouchedAndSpin(List<Way> superLoop)
        {
            for (int i = 0; i < superLoop.Count; i++)
            {
                for (int j = 1; j < superLoop[i].refs.Count; j++)
                {
                    long prev = superLoop[i].refs[j - 1];
                    if (!sector.ContainsCoord(nodes[prev]))
                    {
                        if (j > 1)
                        {
                            // split this way, and add that duplicate node
                            var newWay = new Way();
                            newWay.refs = superLoop[i].refs.Skip(j - 1).ToList();
                            superLoop[i].refs = superLoop[i].refs.Take(j).ToList();
                            superLoop.Insert(i + 1, newWay);
                            superLoop.AddRange(superLoop.Take(i + 1).ToList());
                            superLoop.RemoveRange(0, i + 1);
                        }
                        else
                        {
                            superLoop.AddRange(superLoop.Take(i).ToList());
                            superLoop.RemoveRange(0, i);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private List<long> BreakDownSuperLoop(List<Way> superLoop)
        {
            List<long> refs = new List<long>();
            refs.Add(superLoop.First().refs.First());
            for (int i = 0; i < superLoop.Count; i++)
            {
                for (int j = 1; j < superLoop[i].refs.Count; j++)
                {
                    long next = superLoop[i].refs[j];
                    refs.Add(next);
                }
            }
            return refs;
        }

        internal IEnumerable<Way> EnumerateWays(bool clone = true)
        {
            foreach (var blob in blobs)
            {
                if (blob.type != "OSMData") continue;
                foreach (var pGroup in blob.pBlock.primitivegroup)
                {
                    foreach (var way in pGroup.ways)
                    {
                        way.InitKeyValues(blob.pBlock.stringtable);
                        yield return clone ? Copy(way) : way;
                    }
                }
            }
        }

        internal IEnumerable<Relation> EnumerateRelations()
        {
            foreach (var blob in blobs)
            {
                if (blob.type != "OSMData") continue;
                foreach (var pGroup in blob.pBlock.primitivegroup)
                {
                    foreach (var relation in pGroup.relations)
                    {
                        relation.InitKeyValues(blob.pBlock.stringtable);
                        yield return relation;
                    }
                }
            }
        }

        internal LineGraph GetRoadsFast()
        {
            return GetFast("highway", null, sector);
        }

        internal LineGraph GetFast(string key, string value, ISector sector, bool mergeWays = true)
        {
            RoadInfoVector roads = new RoadInfoVector();
            foreach (var blob in blobs)
            {
                var roadInfo = blob.GetVectors(key, value);
                roads.ways.AddRange(roadInfo.ways);
            }
            LineGraph answer = new LineGraph();
            Dictionary<long, GraphNode> graphNodes = new Dictionary<long, GraphNode>();
            foreach (var way in roads.ways)
            {
                if (!mergeWays) graphNodes = new Dictionary<long, GraphNode>();
                if (way.keyValues.ContainsKey("highway"))
                {
                    if (way.keyValues["highway"] == "footway") continue; // TODO: move this logic
                    if (way.keyValues["highway"] == "cycleway") continue; // TODO: move this logic
                    if (way.keyValues["highway"] == "service") continue; // TODO: move this logic
                }
                long? prev = null;
                // I think I have an idea of whats happened
                // we were expecting simple closed shapes
                // instead we get road like graphs
                // and so when we debug the paths, it probably doesnt know what route to take
                foreach (var nodeRef in way.refs)
                {
                    long? v = nodes.ContainsKey(nodeRef) ? nodeRef : (long?)null;
                    if (v != null && !graphNodes.ContainsKey(v.Value))
                    {
                        var newNode = new GraphNode(nodes[v.Value]);
                        graphNodes[nodeRef] = newNode;
                        answer.nodes.Add(newNode);
                    }
                    if (prev != null && v != null)
                    {
                        graphNodes[prev.Value].nextConnections.Add(graphNodes[v.Value]);
                        graphNodes[prev.Value].nextProps.Add(way.keyValues);
                        graphNodes[v.Value].prevConnections.Add(graphNodes[prev.Value]);
                        graphNodes[v.Value].prevProps.Add(way.keyValues);
                    }
                    prev = v;
                }
            }
            return answer;
        }
        internal LineGraph GetBeachFast()
        {
            return GetFast("natural", "coastline", sector);
        }

        internal LineGraph GetLakesFast()
        {
            return GetFast("natural", "water", sector, false).ForceDirection(true);
        }

        internal LineGraph GetMultiLakesFast()
        {
            return GetLakeMulti();
        }

        // TODO: still issue with relation 2194649, mostly in that its components go off the sector
        // TODO: DEFINITELY need to factor this stuff out in some way to make a unit test
        private LineGraph GetLakeMulti()
        {
            LineGraph finalAnswer = new LineGraph();
            List<List<long>> inners = new List<List<long>>();
            List<List<long>> outers = new List<List<long>>();
            foreach (var blob in blobs)
            {
                if (blob.type != "OSMData") continue;
                int typeIndex = blob.pBlock.stringtable.vals.IndexOf("type");
                int multipolygonIndex = blob.pBlock.stringtable.vals.IndexOf("multipolygon");
                int outerIndex = blob.pBlock.stringtable.vals.IndexOf("outer");
                int innerIndex = blob.pBlock.stringtable.vals.IndexOf("inner");
                int naturalIndex = blob.pBlock.stringtable.vals.IndexOf("natural");
                int waterIndex = blob.pBlock.stringtable.vals.IndexOf("water");
                if (new[] { typeIndex, multipolygonIndex, outerIndex, innerIndex, naturalIndex, waterIndex }.Contains(-1)) continue;
                foreach (var pGroup in blob.pBlock.primitivegroup)
                {
                    foreach (var relation in pGroup.relations)
                    {
                        List<long> innerWayIds = new List<long>();
                        List<long> outerWayIds = new List<long>();
                        bool isNaturalWater = false;
                        bool isTypeMultipolygon = false;
                        for (int i = 0; i < relation.keys.Count; i++)
                        {
                            if (relation.keys[i] == naturalIndex && relation.vals[i] == waterIndex) isNaturalWater = true;
                            if (relation.keys[i] == typeIndex && relation.vals[i] == multipolygonIndex) isTypeMultipolygon = true;
                        }
                        if (isNaturalWater && isTypeMultipolygon)
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
                                        outerWayIds.Add(relation.memids[i]);
                                    }
                                    else
                                    {
                                        if (relation.roles_sid[i] == innerIndex) innerWayIds.Add(relation.memids[i]);
                                        if (relation.roles_sid[i] == outerIndex) outerWayIds.Add(relation.memids[i]);
                                    }
                                }
                            }
                            inners.Add(innerWayIds);
                            outers.Add(outerWayIds);
                        }
                    }
                }
            }
            List<long> allwayIds = new List<long>();
            for (int i = 0; i < inners.Count; i++)
            {
                allwayIds.AddRange(inners[i]);
                allwayIds.AddRange(outers[i]);
            }
            RoadInfoVector roads = new RoadInfoVector();
            foreach (var blob in blobs)
            {
                var roadInfo = blob.GetVectors(allwayIds);
                roads.ways.AddRange(roadInfo.ways);
            }
            for (int i = 0; i < inners.Count; i++)
            {
                finalAnswer = finalAnswer.Combine(MakeThatMultiLake(inners[i], outers[i], roads));
            }
            return finalAnswer;
        }

        private LineGraph MakeThatMultiLake(List<long> innerWayIds, List<long> outerWayIds, RoadInfoVector roads)
        {
            LineGraph inner = MakeThatPolygon(innerWayIds, true, roads);
            LineGraph outer = MakeThatPolygon(outerWayIds, false, roads);
            return inner.ForceDirection(false).Combine(outer.ForceDirection(true));
        }

        private LineGraph MakeThatPolygon(List<long> wayIds, bool isHole, RoadInfoVector roads)
        {
            LineGraph answer = new LineGraph();
            Dictionary<long, GraphNode> graphNodes = new Dictionary<long, GraphNode>();
            foreach (var way in roads.ways)
            {
                if (!wayIds.Contains(way.id)) continue;
                long? prev = null;
                foreach (var nodeRef in way.refs)
                {
                    long? v = nodes.ContainsKey(nodeRef) ? nodeRef : (long?)null;
                    if (v != null && !graphNodes.ContainsKey(v.Value))
                    {
                        var newNode = new GraphNode(nodes[v.Value]);
                        newNode.isHole = isHole;
                        graphNodes[nodeRef] = newNode;
                        answer.nodes.Add(newNode);
                    }
                    if (prev != null && v != null)
                    {
                        if (graphNodes[prev.Value].nextConnections.Contains(graphNodes[v.Value])) // do they already connect?
                        {
                            // if so, undo it (merging polygons, basically)
                            graphNodes[prev.Value].nextConnections.Remove(graphNodes[v.Value]);
                            graphNodes[v.Value].prevConnections.Remove(graphNodes[prev.Value]);
                        }
                        else
                        {
                            graphNodes[prev.Value].nextConnections.Add(graphNodes[v.Value]);
                            graphNodes[v.Value].prevConnections.Add(graphNodes[prev.Value]);
                        }
                    }
                    prev = v;
                }
            }
            return answer.ClosePolygonNaively();
        }
    }
}

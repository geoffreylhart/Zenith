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
    class BlobCollection
    {
        public Dictionary<long, Vector2d> nodes = new Dictionary<long, Vector2d>();
        private List<Blob> blobs;
        private ISector sector;

        public BlobCollection(List<Blob> blobs, ISector sector)
        {
            this.blobs = blobs;
            this.sector = sector;
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

        internal SectorConstrainedOSMAreaMap GetAreaMap(string key, string value)
        {
            throw new NotImplementedException();
        }

        internal SectorConstrainedOSMAreaMap GetCoastAreaMap(string key, string value)
        {
            // remember: "If you regard this as tracing around an area of land, then the coastline way should be running counterclockwise."
            List<List<Way>> linkedWays = null; // gather ways with matching starts/ends to form a super-way, coast ways should always run the same direction, so this becomes easier
            List<List<Way>> loopedWays = new List<List<Way>>(); // fully closed loops
            Dictionary<long, List<Way>> startsWith = new Dictionary<long, List<Way>>();
            Dictionary<long, List<Way>> endsWith = new Dictionary<long, List<Way>>();
            // first, we construct our linkedWays and loopedWays
            foreach (var ourWay in EnumerateWays())
            {
                if (ourWay.keyValues.ContainsKey(key) && ourWay.keyValues[key] == value)
                {
                    long ourFirstNode = ourWay.refs.First();
                    long ourLastNode = ourWay.refs.Last();
                    if (endsWith.ContainsKey(ourFirstNode) && startsWith.ContainsKey(ourLastNode)) // first, try to insert between A & B
                    {
                        var lineA = endsWith[ourFirstNode];
                        var lineB = startsWith[ourLastNode];
                        var lineBLastNode = lineB.Last().refs.Last();
                        if (lineA == lineB) // we've got a closed loop here
                        {
                            lineA.Add(ourWay);
                            loopedWays.Add(lineA);
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
                            loopedWays.Add(us);
                        }
                        else
                        {
                            startsWith[ourFirstNode] = us;
                            endsWith[ourLastNode] = us;
                        }
                    }
                }
            }
            linkedWays = startsWith.Values.ToList();
            // now we actually construct that Sector Constrained OSM Area Map
            SectorConstrainedOSMAreaMap map = new SectorConstrainedOSMAreaMap();
            foreach (var superWay in linkedWays) // we expect these to always start and end outside the sector
            {
                AddConstrainedPaths(map, superWay);
            }
            foreach (var superLoop in loopedWays)
            {
                double area = 0;
                // calculate that area
                Vector2d baseLine = nodes[superLoop.First().refs[1]] - nodes[superLoop.First().refs[0]];
                for (int i = 0; i < superLoop.Count; i++)
                {
                    for (int j = 1; j < superLoop[i].refs.Count; j++)
                    {
                        long prev = superLoop[i].refs[j - 1];
                        long next = superLoop[i].refs[j];
                        Vector2d line2 = nodes[next] - nodes[prev];
                        area += (line2.X * baseLine.Y - line2.Y * baseLine.X) / 2; // random cross-product logic
                    }
                }
                bool isCW = area < 0; // based on the coordinate system we're using, with X right and Y down
                bool untouchedLoop = true;
                for (int i = 0; i < superLoop.Count; i++)
                {
                    for (int j = 1; j < superLoop[i].refs.Count; j++)
                    {
                        long prev = superLoop[i].refs[j - 1];
                        if (!sector.ContainsCoord(nodes[prev]))
                        {
                            untouchedLoop = false;
                            if (j > 1)
                            {
                                // split this way, and add that duplicate node
                                var newWay = new Way();
                                newWay.refs = superLoop[i].refs.Skip(j - 1).ToList();
                                superLoop[i].refs = superLoop[i].refs.Take(j).ToList();
                                superLoop.Insert(i + 1, newWay);
                                superLoop.AddRange(superLoop.Take(i + 1));
                                superLoop.RemoveRange(0, i + 1);
                            }
                            else
                            {
                                superLoop.AddRange(superLoop.Take(i));
                                superLoop.RemoveRange(0, i);
                            }
                            AddConstrainedPaths(map, superLoop);
                            break;
                        }
                    }
                    if (!untouchedLoop) break;
                }
                if (untouchedLoop)
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
                    if (isCW)
                    {
                        map.inners.Add(refs);
                    }
                    else
                    {
                        map.outers.Add(refs);
                    }
                }
            }
            // done, wow, so much work
            return map;
        }

        private void AddConstrainedPaths(SectorConstrainedOSMAreaMap map, List<Way> superWay)
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
                    var intersections = OSMPolygonBufferGenerator.GetIntersections(sector, nodes[prev], nodes[next]);
                    foreach (var intersection in intersections)
                    {
                        if (prevIsInside) // close-out a line
                        {
                            map.paths.Last().end = intersection;
                        }
                        else // start up a new line
                        {
                            SectorConstrainedOSMPath newPath = new SectorConstrainedOSMPath();
                            newPath.start = intersection;
                            map.paths.Add(newPath);
                        }
                        prevIsInside = !prevIsInside;
                    }
                    if (prevIsInside)
                    {
                        map.paths.Last().nodeRefs.Add(next);
                    }
                }
            }
        }

        internal IEnumerable<Way> EnumerateWays()
        {
            foreach (var blob in blobs)
            {
                if (blob.type != "OSMData") continue;
                RoadInfoVector info = new RoadInfoVector();
                foreach (var pGroup in blob.pBlock.primitivegroup)
                {
                    foreach (var way in pGroup.ways)
                    {
                        way.InitKeyValues(blob.pBlock.stringtable);
                        yield return way;
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
                                    if (relation.roles_sid[i] == innerIndex) innerWayIds.Add(relation.memids[i]);
                                    if (relation.roles_sid[i] == outerIndex) outerWayIds.Add(relation.memids[i]);
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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZGeom;
using Zenith.ZMath;
using static Zenith.LibraryWrappers.OSM.Blob;
using static Zenith.ZGeom.LineGraph;

namespace Zenith.LibraryWrappers.OSM
{
    // responsible for lots of stuff
    class BlobCollection
    {
        private List<Blob> blobs;
        private ISector sector;

        public BlobCollection(List<Blob> blobs, ISector sector)
        {
            this.blobs = blobs;
            this.sector = sector;
        }

        internal void Init()
        {
            // throw new NotImplementedException();
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
                var roadInfo = blob.GetVectors(key, value, sector);
                roads.ways.AddRange(roadInfo.ways);
                foreach (var pair in roadInfo.nodes) roads.nodes.Add(pair.Key, pair.Value);
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
                    long? v = roads.nodes.ContainsKey(nodeRef) ? nodeRef : (long?)null;
                    if (v != null && !graphNodes.ContainsKey(v.Value))
                    {
                        var newNode = new GraphNode(roads.nodes[v.Value]);
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
            return GetFast("natural", "water", sector, false);
        }

        internal LineGraph GetMultiLakesFast()
        {
            return GetLakeMulti();
        }

        // TODO: still issue with relation 2194649, mostly in that its components go off the sector
        // TODO: DEFINITELY need to factor this stuff out in some way to make a unit test
        private LineGraph GetLakeMulti()
        {
            List<long> innerWayIds = new List<long>();
            List<long> outerWayIds = new List<long>();
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
                        }
                    }
                }
            }
            RoadInfoVector roadsInner = new RoadInfoVector();
            foreach (var blob in blobs)
            {
                var roadInfo = blob.GetVectors(innerWayIds, sector);
                roadsInner.ways.AddRange(roadInfo.ways);
                foreach (var pair in roadInfo.nodes) roadsInner.nodes.Add(pair.Key, pair.Value);
            }
            RoadInfoVector roadsOuter = new RoadInfoVector();
            foreach (var blob in blobs)
            {
                var roadInfo = blob.GetVectors(outerWayIds, sector);
                roadsOuter.ways.AddRange(roadInfo.ways);
                foreach (var pair in roadInfo.nodes) roadsOuter.nodes.Add(pair.Key, pair.Value);
            }
            LineGraph answerOuter = new LineGraph();
            Dictionary<long, GraphNode> graphNodes = new Dictionary<long, GraphNode>();
            foreach (var way in roadsOuter.ways)
            {
                long? prev = null;
                foreach (var nodeRef in way.refs)
                {
                    long? v = roadsOuter.nodes.ContainsKey(nodeRef) ? nodeRef : (long?)null;
                    if (v != null && !graphNodes.ContainsKey(v.Value))
                    {
                        var newNode = new GraphNode(roadsOuter.nodes[v.Value]);
                        graphNodes[nodeRef] = newNode;
                        answerOuter.nodes.Add(newNode);
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
            LineGraph answerInner = new LineGraph();
            foreach (var way in roadsInner.ways)
            {
                long? prev = null;
                foreach (var nodeRef in way.refs)
                {
                    long? v = roadsInner.nodes.ContainsKey(nodeRef) ? nodeRef : (long?)null;
                    if (v != null && !graphNodes.ContainsKey(v.Value))
                    {
                        var newNode = new GraphNode(roadsInner.nodes[v.Value]);
                        newNode.isHole = true;
                        graphNodes[nodeRef] = newNode;
                        answerInner.nodes.Add(newNode);
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
            return answerOuter.ForceDirection(true).Combine(answerInner.ForceDirection(false));
        }
    }
}

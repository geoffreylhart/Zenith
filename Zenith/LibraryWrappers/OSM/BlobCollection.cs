using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZGeom;
using static Zenith.LibraryWrappers.OSM.Blob;
using static Zenith.ZGeom.LineGraph;

namespace Zenith.LibraryWrappers.OSM
{
    // responsible for lots of stuff
    class BlobCollection
    {
        private List<Blob> blobs;

        public BlobCollection(List<Blob> blobs)
        {
            this.blobs = blobs;
        }

        internal void Init()
        {
            // throw new NotImplementedException();
        }

        internal LineGraph GetRoadsFast()
        {
            return GetFast("highway", null);
        }

        internal LineGraph GetFast(string key, string value)
        {
            RoadInfoVector roads = new RoadInfoVector();
            foreach (var blob in blobs)
            {
                var roadInfo = blob.GetVectors(key, value);
                roads.refs.AddRange(roadInfo.refs);
                foreach (var pair in roadInfo.nodes) roads.nodes.Add(pair.Key, pair.Value);
            }
            LineGraph answer = new LineGraph();
            Dictionary<long, GraphNode> graphNodes = new Dictionary<long, GraphNode>();
            foreach (var way in roads.refs)
            {
                long? prev = null;
                foreach (var nodeRef in way)
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
                        graphNodes[v.Value].prevConnections.Add(graphNodes[prev.Value]);
                    }
                    prev = v;
                }
            }
            return answer;
        }
        internal LineGraph GetBeachFast()
        {
            return GetFast("natural", "coastline");
        }

        internal LineGraph GetLakesFast()
        {
            // TODO: handles those multipolygon lakes
            return GetFast("natural", "water").Combine(GetLakeMulti());
        }

        private LineGraph GetLakeMulti()
        {
            List<long> wayIds = new List<long>();
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
                                if (relation.roles_sid[i] == outerIndex && relation.types[i] == 1)
                                {
                                    wayIds.Add(relation.memids[i]);
                                }
                            }
                        }
                    }
                }
            }
            RoadInfoVector roads = new RoadInfoVector();
            foreach (var blob in blobs)
            {
                var roadInfo = blob.GetVectors(wayIds);
                roads.refs.AddRange(roadInfo.refs);
                foreach (var pair in roadInfo.nodes) roads.nodes.Add(pair.Key, pair.Value);
            }
            LineGraph answer = new LineGraph();
            Dictionary<long, GraphNode> graphNodes = new Dictionary<long, GraphNode>();
            foreach (var way in roads.refs)
            {
                long? prev = null;
                foreach (var nodeRef in way)
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
                        graphNodes[v.Value].prevConnections.Add(graphNodes[prev.Value]);
                    }
                    prev = v;
                }
            }
            return answer;
        }
    }
}

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
            return GetFast("natural", "water");
        }
    }
}

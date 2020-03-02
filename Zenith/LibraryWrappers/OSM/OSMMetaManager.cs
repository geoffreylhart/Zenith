using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    // load, save, and process quick lookups for massive items
    class OSMMetaManager
    {
        Dictionary<string, EdgeInfo> edgeInfo = new Dictionary<string, EdgeInfo>();
        Dictionary<long, WayInfo> wayInfo = new Dictionary<long, WayInfo>();
        Dictionary<long, RelationInfo> relationInfo = new Dictionary<long, RelationInfo>();

        internal void SaveAll(int i)
        {
            string filePath = Path.Combine(OSMPaths.GetLocalCacheRoot(), "planet-meta" + i + ".data");
            using (var writer = File.Open(filePath, FileMode.Create))
            {
                using (var bw = new BinaryWriter(writer))
                {
                    bw.Write(edgeInfo.Count);
                    foreach (var e in edgeInfo.Values)
                    {
                        bw.Write(e.wayID);
                        bw.Write(e.node1);
                        bw.Write(e.node2);
                        bw.Write(e.longLat1.X);
                        bw.Write(e.longLat1.Y);
                        bw.Write(e.longLat2.X);
                        bw.Write(e.longLat2.Y);
                    }
                    bw.Write(wayInfo.Count);
                    foreach (var w in wayInfo.Values)
                    {
                        bw.Write(w.id);
                        bw.Write(w.keyValues.Count);
                        foreach (var pair in w.keyValues)
                        {
                            bw.Write(pair.Key);
                            bw.Write(pair.Value);
                        }
                    }
                    bw.Write(relationInfo.Count);
                    foreach (var r in relationInfo.Values)
                    {
                        bw.Write(r.id);
                        bw.Write(r.keyValues.Count);
                        foreach (var pair in r.keyValues)
                        {
                            bw.Write(pair.Key);
                            bw.Write(pair.Value);
                        }
                        bw.Write(r.roleValues.Count);
                        foreach (var rolveValue in r.roleValues) bw.Write(rolveValue);
                        bw.Write(r.memids.Count);
                        foreach (var memid in r.memids) bw.Write(memid);
                        bw.Write(r.types.Count);
                        foreach (var type in r.types) bw.Write(type);
                    }
                }
            }
        }

        // took 20.178 hours to save 1.28 GB
        public void LoadAllDetailsFromSource()
        {
            int i = 0;
            foreach (CubeSector root in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                for (int x = 0; x < 256; x++)
                {
                    for (int y = 0; y < 256; y++)
                    {
                        var sector = new CubeSector(root.sectorFace, x, y, 8);
                        LoadDetailsFromSource(sector);
                    }
                }
                SaveAll(i);
                Clear();
                i++;
            }
        }

        private void Clear()
        {
            edgeInfo = new Dictionary<string, EdgeInfo>();
            wayInfo = new Dictionary<long, WayInfo>();
            relationInfo = new Dictionary<long, RelationInfo>();
        }

        private void LoadDetailsFromSource(ISector sector)
        {
            BlobCollection blobs = OSMReader.GetAllBlobs(sector);
            foreach (var way in blobs.EnumerateWays(false))
            {
                for (int i = 0; i < way.refs.Count; i++)
                {
                    long ref1 = way.refs[i];
                    long ref2 = way.refs[(i + 1) % way.refs.Count];
                    if (ref1 == ref2) continue;
                    Vector2d v1 = blobs.nodes[ref1];
                    Vector2d v2 = blobs.nodes[ref2];
                    LongLat longLat1 = new SphereVector(sector.ProjectToSphereCoordinates(v1)).ToLongLat();
                    LongLat longLat2 = new SphereVector(sector.ProjectToSphereCoordinates(v2)).ToLongLat();
                    ISector sector1 = GetContainingSector(longLat1, 8);
                    ISector sector2 = GetContainingSector(longLat2, 8);
                    if (!sector1.Equals(sector2))
                    {
                        var e = new EdgeInfo() { wayID = way.id, node1 = ref1, node2 = ref2, longLat1 = longLat1, longLat2 = longLat2 };
                        string eKey = e.wayID + "," + e.node1 + "," + e.node2;
                        if (!edgeInfo.ContainsKey(eKey)) edgeInfo[eKey] = e;
                        if (!wayInfo.ContainsKey(way.id)) wayInfo[way.id] = new WayInfo() { id = way.id, keyValues = way.keyValues };
                    }
                }
            }
            foreach (var relation in blobs.EnumerateRelations())
            {
                foreach (var way in relation.memids)
                {
                    if (wayInfo.ContainsKey(way) && !relationInfo.ContainsKey(relation.id))
                    {
                        relationInfo[relation.id] = new RelationInfo() { id = relation.id, keyValues = relation.keyValues, roleValues = relation.roleValues, memids = relation.memids, types = relation.types };
                    }
                }
            }
        }

        private ISector GetContainingSector(LongLat x, int level)
        {
            foreach (var root in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                if (root.ContainsLongLat(x))
                {
                    Vector2d localAgain = root.ProjectToLocalCoordinates(x.ToSphereVector());
                    return root.GetSectorAt(localAgain.X, localAgain.Y, level);
                }
            }
            throw new NotImplementedException();
        }

        public class EdgeInfo
        {
            public long wayID;
            public long node1;
            public long node2;
            public LongLat longLat1;
            public LongLat longLat2;
        }

        public class WayInfo
        {
            public long id;
            public Dictionary<string, string> keyValues;
        }

        // just stash all relation info, just in case
        public class RelationInfo
        {
            public long id;
            public Dictionary<string, string> keyValues;
            public List<string> roleValues;
            public List<long> memids;
            public List<int> types;
        }
    }
}

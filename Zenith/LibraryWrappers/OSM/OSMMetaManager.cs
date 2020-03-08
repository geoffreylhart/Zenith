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
    public class OSMMetaManager
    {
        public HashSet<EdgeInfo> edgeInfo = new HashSet<EdgeInfo>();
        public Dictionary<long, WayInfo> wayInfo = new Dictionary<long, WayInfo>();
        public Dictionary<long, RelationInfo> relationInfo = new Dictionary<long, RelationInfo>();

        internal void SaveAll(string fileName)
        {
            string filePath = Path.Combine(OSMPaths.GetLocalCacheRoot(), fileName);
            using (var writer = File.Open(filePath, FileMode.Create))
            {
                using (var bw = new BinaryWriter(writer))
                {
                    bw.Write(edgeInfo.Count);
                    foreach (var e in edgeInfo)
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
                        bw.Write(w.startNode);
                        bw.Write(w.endNode);
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
                        foreach (var roleValue in r.roleValues) bw.Write(roleValue);
                        bw.Write(r.memids.Count);
                        foreach (var memid in r.memids) bw.Write(memid);
                        bw.Write(r.types.Count);
                        foreach (var type in r.types) bw.Write(type);
                    }
                }
            }
        }

        internal void LoadAll(string fileName)
        {
            string filePath = Path.Combine(OSMPaths.GetLocalCacheRoot(), fileName);
            using (var reader = File.Open(filePath, FileMode.Open))
            {
                using (var br = new BinaryReader(reader))
                {
                    int edgeInfoCount = br.ReadInt32();
                    for (int j = 0; j < edgeInfoCount; j++)
                    {
                        EdgeInfo e = new EdgeInfo();
                        e.wayID = br.ReadInt64();
                        e.node1 = br.ReadInt64();
                        e.node2 = br.ReadInt64();
                        e.longLat1 = new LongLat(br.ReadDouble(), br.ReadDouble());
                        e.longLat2 = new LongLat(br.ReadDouble(), br.ReadDouble());
                        if (!edgeInfo.Contains(e)) edgeInfo.Add(e);
                    }
                    int wayInfoCount = br.ReadInt32();
                    for (int j = 0; j < wayInfoCount; j++)
                    {
                        WayInfo w = new WayInfo();
                        w.id = br.ReadInt64();
                        w.startNode = br.ReadInt64();
                        w.endNode = br.ReadInt64();
                        int keyValueCount = br.ReadInt32();
                        w.keyValues = new Dictionary<string, string>();
                        for (int k = 0; k < keyValueCount; k++)
                        {
                            w.keyValues[br.ReadString()] = br.ReadString();
                        }
                        if (!wayInfo.ContainsKey(w.id)) wayInfo[w.id] = w;
                    }
                    int relationInfoCount = br.ReadInt32();
                    for (int j = 0; j < relationInfoCount; j++)
                    {
                        RelationInfo r = new RelationInfo();
                        r.id = br.ReadInt64();
                        int keyValueCount = br.ReadInt32();
                        r.keyValues = new Dictionary<string, string>();
                        for (int k = 0; k < keyValueCount; k++)
                        {
                            r.keyValues[br.ReadString()] = br.ReadString();
                        }
                        int roleValuesCount = br.ReadInt32();
                        r.roleValues = new List<string>();
                        for (int k = 0; k < roleValuesCount; k++) r.roleValues.Add(br.ReadString());
                        int memidsCount = br.ReadInt32();
                        r.memids = new List<long>();
                        for (int k = 0; k < memidsCount; k++) r.memids.Add(br.ReadInt64());
                        int typesCount = br.ReadInt32();
                        r.types = new List<int>();
                        for (int k = 0; k < typesCount; k++) r.types.Add(br.ReadInt32());
                        // if (!relationInfo.ContainsKey(r.id)) relationInfo[r.id] = r; // disabling to save some memory, sure
                    }
                }
            }
        }

        // took 21.117584 hours to save 4.19 GB
        // saved to 6 different files in case it crashes
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
                SaveAll("planet-meta" + i + ".data");
                Clear();
                i++;
            }
        }

        // uses a minute of time and maybe 7 gigs of memory to comine those 6 files, file only shrinks from 1.28 -> 1.26 GB
        public void CombineFiles()
        {
            for (int i = 0; i < 6; i++)
            {
                LoadAll("planet-meta" + i + ".data");
            }
            SaveAll("planet-meta.data");
        }

        private void Clear()
        {
            edgeInfo = new HashSet<EdgeInfo>();
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
                        if (!edgeInfo.Contains(e)) edgeInfo.Add(e);
                        var w = new WayInfo() { id = way.id, keyValues = way.keyValues, startNode = way.refs.First(), endNode = way.refs.Last() };
                        if (!wayInfo.ContainsKey(w.id)) wayInfo[w.id] = w;
                    }
                }
            }
            HashSet<long> extraWays = new HashSet<long>();
            foreach (var relation in blobs.EnumerateRelations())
            {
                foreach (var way in relation.memids)
                {
                    extraWays.Add(way);
                    if (wayInfo.ContainsKey(way) && !relationInfo.ContainsKey(relation.id))
                    {
                        var r = new RelationInfo() { id = relation.id, keyValues = relation.keyValues, roleValues = relation.roleValues, memids = relation.memids, types = relation.types };
                        relationInfo[r.id] = r;
                    }
                }
            }
            foreach (var way in blobs.EnumerateWays(false))
            {
                if (!extraWays.Contains(way.id)) continue;
                var w = new WayInfo() { id = way.id, keyValues = way.keyValues, startNode = way.refs.First(), endNode = way.refs.Last() };
                if (!wayInfo.ContainsKey(w.id)) wayInfo[w.id] = w;
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

            public override int GetHashCode()
            {
                return GetKey().GetHashCode();
            }

            public override bool Equals(object that)
            {
                return GetKey().Equals(((EdgeInfo)that).GetKey());
            }

            private string GetKey()
            {
                string eKey = wayID + "," + node1 + "," + node2;
                return eKey;
            }
        }

        public class WayInfo
        {
            public long id;
            public long startNode;
            public long endNode;
            public Dictionary<string, string> keyValues;

            public override int GetHashCode()
            {
                return id.GetHashCode();
            }

            public override bool Equals(object that)
            {
                return id.Equals(((WayInfo)that).id);
            }
        }

        // just stash all relation info, just in case
        public class RelationInfo
        {
            public long id;
            public Dictionary<string, string> keyValues;
            public List<string> roleValues;
            public List<long> memids;
            public List<int> types;

            public override int GetHashCode()
            {
                return id.GetHashCode();
            }

            public override bool Equals(object that)
            {
                return id.Equals(((RelationInfo)that).id);
            }
        }
    }
}

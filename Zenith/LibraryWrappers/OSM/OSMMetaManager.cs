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

        internal void SaveAll(string fileName)
        {
            string filePath = Path.Combine(OSMPaths.GetLocalCacheRoot(), fileName);
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
                        string eKey = e.wayID + "," + e.node1 + "," + e.node2;
                        if (!edgeInfo.ContainsKey(eKey)) edgeInfo[eKey] = e;
                    }
                    int wayInfoCount = br.ReadInt32();
                    for (int j = 0; j < wayInfoCount; j++)
                    {
                        WayInfo w = new WayInfo();
                        w.id = br.ReadInt64();
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
                        if (!relationInfo.ContainsKey(r.id)) relationInfo[r.id] = r;
                    }
                }
            }
        }

        // took 20.178 hours to save 1.28 GB
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

        // uses a minute of time and maybe 8 gigs of memory to comine those 6 files, file only shrinks from 1.28 -> 1.26 GB
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

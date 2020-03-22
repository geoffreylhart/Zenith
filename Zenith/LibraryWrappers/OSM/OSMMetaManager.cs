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
        public List<string> stringTable = new List<string>();
        public Dictionary<string, int> stringTableLookup = new Dictionary<string, int>();
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
                        bw.Write(w.keys.Count);
                        for (int i = 0; i < w.keys.Count; i++)
                        {
                            bw.Write(stringTable[w.keys[i]]);
                            bw.Write(stringTable[w.values[i]]);
                        }
                    }
                    bw.Write(relationInfo.Count);
                    foreach (var r in relationInfo.Values)
                    {
                        bw.Write(r.id);
                        bw.Write(r.keys.Count);
                        for (int i = 0; i < r.keys.Count; i++)
                        {
                            bw.Write(stringTable[r.keys[i]]);
                            bw.Write(stringTable[r.values[i]]);
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
                        w.keys = new List<int>();
                        w.values = new List<int>();
                        w.relations = new List<long>();
                        for (int k = 0; k < keyValueCount; k++)
                        {
                            w.keys.Add(LoadIntoStringTable(br.ReadString()));
                            w.values.Add(LoadIntoStringTable(br.ReadString()));
                        }
                        if (!wayInfo.ContainsKey(w.id)) wayInfo[w.id] = w;
                    }
                    int relationInfoCount = br.ReadInt32();
                    for (int j = 0; j < relationInfoCount; j++)
                    {
                        RelationInfo r = new RelationInfo();
                        r.id = br.ReadInt64();
                        int keyValueCount = br.ReadInt32();
                        r.keys = new List<int>();
                        r.values = new List<int>();
                        for (int k = 0; k < keyValueCount; k++)
                        {
                            r.keys.Add(LoadIntoStringTable(br.ReadString()));
                            r.values.Add(LoadIntoStringTable(br.ReadString()));
                        }
                        int roleValuesCount = br.ReadInt32();
                        r.roleValues = new List<int>();
                        for (int k = 0; k < roleValuesCount; k++) r.roleValues.Add(LoadIntoStringTable(br.ReadString()));
                        int memidsCount = br.ReadInt32();
                        r.memids = new List<long>();
                        for (int k = 0; k < memidsCount; k++)
                        {
                            r.memids.Add(br.ReadInt64());
                            if (wayInfo.ContainsKey(r.memids.Last())) wayInfo[r.memids.Last()].relations.Add(r.id);
                        }
                        int typesCount = br.ReadInt32();
                        r.types = new List<int>();
                        for (int k = 0; k < typesCount; k++) r.types.Add(br.ReadInt32());
                        if (!relationInfo.ContainsKey(r.id)) relationInfo[r.id] = r;
                    }
                }
            }
        }

        private int LoadIntoStringTable(string str)
        {
            if (!stringTableLookup.ContainsKey(str))
            {
                stringTable.Add(str);
                stringTableLookup[str] = stringTable.Count - 1;
            }
            return stringTableLookup[str];
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
            stringTable = new List<string>();
            stringTableLookup = new Dictionary<string, int>();
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
                    // hmm, this logic will ignore edges that brush up exactly against the top or left of their sector (ex: nodes 6151473219, 6151473220 in way 146849673)
                    // I have to compensate for this elswhere
                    ISector sector1 = GetContainingSector(longLat1, 8);
                    ISector sector2 = GetContainingSector(longLat2, 8);
                    if (!sector1.Equals(sector2))
                    {
                        var e = new EdgeInfo() { wayID = way.id, node1 = ref1, node2 = ref2, longLat1 = longLat1, longLat2 = longLat2 };
                        if (!edgeInfo.Contains(e)) edgeInfo.Add(e);
                        List<int> wKeys = new List<int>();
                        List<int> wVals = new List<int>();
                        foreach (var pair in way.keyValues)
                        {
                            wKeys.Add(LoadIntoStringTable(pair.Key));
                            wVals.Add(LoadIntoStringTable(pair.Value));
                        }
                        var w = new WayInfo() { id = way.id, keys = wKeys, values = wVals, startNode = way.refs.First(), endNode = way.refs.Last(), relations = new List<long>() };
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
                        List<int> rKeys = new List<int>();
                        List<int> rVals = new List<int>();
                        List<int> rRoles = new List<int>();
                        foreach (var pair in relation.keyValues)
                        {
                            rKeys.Add(LoadIntoStringTable(pair.Key));
                            rVals.Add(LoadIntoStringTable(pair.Value));
                        }
                        foreach (var role in relation.roleValues)
                        {
                            rRoles.Add(LoadIntoStringTable(role));
                        }
                        var r = new RelationInfo() { id = relation.id, keys = rKeys, values = rVals, roleValues = rRoles, memids = relation.memids, types = relation.types };
                        relationInfo[r.id] = r;
                    }
                }
            }
            foreach (var way in blobs.EnumerateWays(false))
            {
                if (!extraWays.Contains(way.id)) continue;
                List<int> wKeys = new List<int>();
                List<int> wVals = new List<int>();
                foreach (var pair in way.keyValues)
                {
                    wKeys.Add(LoadIntoStringTable(pair.Key));
                    wVals.Add(LoadIntoStringTable(pair.Value));
                }
                var w = new WayInfo() { id = way.id, keys = wKeys, values = wVals, startNode = way.refs.First(), endNode = way.refs.Last() };
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
            public List<int> keys;
            public List<int> values;
            public List<long> relations;

            public override int GetHashCode()
            {
                return id.GetHashCode();
            }

            public override bool Equals(object that)
            {
                return id.Equals(((WayInfo)that).id);
            }

            internal bool ContainsKeyValue(OSMMetaManager manager, string key, string val)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    if (manager.stringTable[keys[i]].Equals(key) && manager.stringTable[values[i]].Equals(val)) return true;
                }
                return false;
            }
        }

        // just stash all relation info, just in case
        public class RelationInfo
        {
            public long id;
            public List<int> keys;
            public List<int> values;
            public List<int> roleValues;
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

            internal bool ContainsKeyValue(OSMMetaManager manager, string key, string val)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    if (manager.stringTable[keys[i]].Equals(key) && manager.stringTable[values[i]].Equals(val)) return true;
                }
                return false;
            }
        }
    }
}

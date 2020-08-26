using System.Collections.Generic;
using System.IO;
using Zenith.Utilities;

namespace Zenith.LibraryWrappers.OSM
{
    public class Blob
    {
        public int length;
        public string type; // 10
        public byte[] indexdata; // 18
        public int datasize; // 24
        public byte[] raw; // 10
        public int raw_size; // 16
        public byte[] zlib_data; // 26

        // parsed stuff
        public PrimitiveBlock pBlock;

        internal RoadInfoVector GetVectors(string key, string value)
        {
            if (type != "OSMData") return new RoadInfoVector();
            RoadInfoVector info = new RoadInfoVector();
            int highwayIndex = pBlock.stringtable.vals.IndexOf(key);
            int? valueIndex = value == null ? (int?)null : pBlock.stringtable.vals.IndexOf(value);
            foreach (var pGroup in pBlock.primitivegroup)
            {
                foreach (var way in pGroup.ways)
                {
                    if (way.keys.Contains(highwayIndex) && (valueIndex == null || way.vals.Contains(valueIndex.Value)))
                    {
                        way.InitKeyValues(pBlock.stringtable);
                        info.ways.Add(way);
                    }
                }
            }
            return info;
        }

        internal RoadInfoVector GetVectors(List<long> ids)
        {
            HashSet<long> idHash = new HashSet<long>();
            foreach (var id in ids) idHash.Add(id);
            if (type != "OSMData") return new RoadInfoVector();
            RoadInfoVector info = new RoadInfoVector();
            foreach (var pGroup in pBlock.primitivegroup)
            {
                foreach (var way in pGroup.ways)
                {
                    if (idHash.Contains(way.id))
                    {
                        info.ways.Add(way);
                    }
                }
            }
            return info;
        }

        internal void Init()
        {
            if (type != "OSMData") return;
            zlib_data = Compression.UnZLibToBytes(zlib_data, raw_size);
            pBlock = PrimitiveBlock.Read(new MemoryStream(zlib_data));
        }

        internal class RoadInfoVector
        {
            public List<Way> ways = new List<Way>();
        }
    }
}

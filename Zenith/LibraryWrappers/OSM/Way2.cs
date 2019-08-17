using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    public class RawWay
    {
        public long id; // 1
        public List<int> keys = new List<int>(); // 2, packed
        public List<int> vals = new List<int>(); // 3, packed
        public Info info; // 4
        public List<long> refs = new List<long>(); // 8, packed, signed, delta coded

        internal static RawWay Read(Stream stream)
        {
            RawWay obj = new RawWay();
            long lengthInBytes = OSMReader.ReadVarInt(stream);
            long end = stream.Position + lengthInBytes;
            int b = stream.ReadByte();
            if (b != 8) throw new NotImplementedException();
            obj.id = OSMReader.ReadVarInt(stream);
            b = stream.ReadByte();
            if (b == 18)
            {
                obj.keys = OSMReader.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 26)
            {
                obj.vals = OSMReader.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 34)
            {
                //obj.info = Info.Read(stream);
                OSMReader.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 66)
            {
                obj.refs = OSMReader.ReadPackedDeltaCodedVarInts(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            throw new NotImplementedException();
        }

        public Dictionary<string, string> keyValues = new Dictionary<string, string>();

        internal void InitKeyValues(StringTable stringtable)
        {
            for(int i = 0; i < keys.Count; i++)
            {
                keyValues[stringtable.vals[keys[i]]] = stringtable.vals[vals[i]];
            }
        }
    }
}

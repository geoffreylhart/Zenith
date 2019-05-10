using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    class RawWay
    {
        public long id; // 1
        public List<int> keys = new List<int>(); // 2, packed
        public List<int> vals = new List<int>(); // 3, packed
        public Info info; // 4
        public List<long> refs = new List<long>(); // 8, packed, signed, delta coded

        internal static RawWay Read(Stream stream, int keyFilter, int? valueFilter)
        {
            RawWay obj = new RawWay();
            long lengthInBytes = OSM.ReadVarInt(stream);
            long end = stream.Position + lengthInBytes;
            int b = stream.ReadByte();
            if (b != 8) throw new NotImplementedException();
            obj.id = OSM.ReadVarInt(stream);
            b = stream.ReadByte();
            if (b == 18)
            {
                obj.keys = OSM.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (!obj.keys.Contains(keyFilter)) stream.Seek(end, SeekOrigin.Begin);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 26)
            {
                if (valueFilter == null)
                {
                    OSM.SkipBytes(stream);
                }
                else
                {
                    obj.vals = OSM.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                    if (!obj.vals.Contains(valueFilter.Value)) stream.Seek(end, SeekOrigin.Begin);
                }
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 34)
            {
                //obj.info = Info.Read(stream);
                OSM.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 66)
            {
                obj.refs = OSM.ReadPackedDeltaCodedVarInts(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            throw new NotImplementedException();
        }
    }
}

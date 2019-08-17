using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    public class DenseInfo
    {
        public List<int> version; // 1, packed
        public List<long> timestamp; // 2, packed, signed, delta coded
        public List<long> changeset; // 3, packed, signed, delta coded
        public List<int> uid; // 4, packed, signed, delta coded
        public List<int> user_sid; // 5, packed, signed, delta coded
        public List<bool> visible; // 6, packed

        internal static DenseInfo Read(Stream stream)
        {
            DenseInfo obj = new DenseInfo();
            long lengthInBytes = OSMReader.ReadVarInt(stream);
            long end = stream.Position + lengthInBytes;
            int b = stream.ReadByte();
            if (b == 10)
            {
                obj.version = OSMReader.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 18)
            {
                obj.timestamp = OSMReader.ReadPackedDeltaCodedVarInts(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 26)
            {
                obj.changeset = OSMReader.ReadPackedDeltaCodedVarInts(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 34)
            {
                obj.uid = OSMReader.ReadPackedDeltaCodedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 42)
            {
                obj.user_sid = OSMReader.ReadPackedDeltaCodedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 50)
            {
                OSMReader.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            throw new NotImplementedException();
        }
    }
}

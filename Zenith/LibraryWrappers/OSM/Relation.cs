using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    class Relation
    {
        public long id; // 1
        public List<int> keys = new List<int>(); // 2, packed
        public List<int> vals = new List<int>(); // 3, packed
        public Info info; // 4
        public List<int> roles_sid = new List<int>(); // 8, packed
        public List<long> memids = new List<long>(); // 9, packed, signed, delta coded
        public List<int> types = new List<int>(); // 10, packed (0=NODE,1=WAY,2=RELATION)

        internal static Relation Read(Stream stream)
        {
            Relation obj = new Relation();
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
                obj.roles_sid = OSMReader.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 74)
            {
                obj.memids = OSMReader.ReadPackedDeltaCodedVarInts(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 82)
            {
                obj.types = OSMReader.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            throw new NotImplementedException();
        }
    }
}

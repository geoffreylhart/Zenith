using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    public class DenseNodes
    {
        public List<long> id = new List<long>(); // 1, packed, signed, delta coded
        public DenseInfo denseinfo; // 5
        public List<long> lat = new List<long>(); // 8, packed, signed, delta coded
        public List<long> lon = new List<long>(); // 9, packed, signed, delta coded
        public List<int> keys_vals = new List<int>(); // 10, packed

        internal static DenseNodes Read(Stream stream)
        {
            DenseNodes obj = new DenseNodes();
            long lengthInBytes = OSMReader.ReadVarInt(stream);
            long end = stream.Position + lengthInBytes;
            int b = stream.ReadByte();
            if (b == 10)
            {
                obj.id = OSMReader.ReadPackedDeltaCodedVarInts(stream);
                b = stream.ReadByte();
            }
            if (b == 42)
            {
                //obj.denseinfo = DenseInfo.Read(stream);
                OSMReader.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 66)
            {
                obj.lat = OSMReader.ReadPackedDeltaCodedVarInts(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 74)
            {
                obj.lon = OSMReader.ReadPackedDeltaCodedVarInts(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 82)
            {
                //obj.keys_vals = OSM.ReadPackedVarInts(stream).ConvertAll(x => (int)x).ToList();
                OSMReader.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            throw new NotImplementedException();
        }

        internal static DenseNodes ReadNodeStart(MemoryStream stream)
        {
            DenseNodes obj = new DenseNodes();
            long lengthInBytes = OSMReader.ReadVarInt(stream);
            long end = stream.Position + lengthInBytes;
            int b = stream.ReadByte();
            if (b == 10)
            {
                obj.id = new List<long>();
                long length = OSMReader.ReadVarInt(stream);
                if (length > 0)
                {
                    obj.id.Add(OSMReader.ReadSignedVarInt(stream));
                }
                stream.Seek(end, SeekOrigin.Begin);
                return obj;
            }
            if (b == 42)
            {
                OSMReader.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 66)
            {
                OSMReader.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 74)
            {
                OSMReader.SkipBytes(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 82)
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

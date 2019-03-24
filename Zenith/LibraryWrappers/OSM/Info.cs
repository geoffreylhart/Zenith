using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    class Info
    {
        public int version = -1; // 1
        public int timestamp; // 2
        public long changeset; // 3
        public int uid; // 4
        public int user_sid; // 5
        public bool visible; // 6

        internal static Info Read(Stream stream)
        {
            Info obj = new Info();
            long lengthInBytes = OSM.ReadVarInt(stream);
            long end = stream.Position + lengthInBytes;
            int b = stream.ReadByte();
            if (b == 8)
            {
                obj.version = (int)OSM.ReadVarInt(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 16)
            {
                obj.timestamp = (int)OSM.ReadVarInt(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 24)
            {
                obj.changeset = OSM.ReadVarInt(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 32)
            {
                obj.uid = (int)OSM.ReadVarInt(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 40)
            {
                obj.user_sid = (int)OSM.ReadVarInt(stream);
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            if (b == 48)
            {
                obj.visible = OSM.ReadVarInt(stream) == 1;
                if (stream.Position > end) throw new NotImplementedException();
                if (stream.Position == end) return obj;
                b = stream.ReadByte();
            }
            throw new NotImplementedException();
        }
    }
}

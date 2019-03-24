using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    class StringTable
    {
        public List<string> vals;

        internal static StringTable Read(Stream stream)
        {
            StringTable obj = new StringTable();
            long length = OSM.ReadVarInt(stream);
            long end = stream.Position + length;
            obj.vals = new List<string>();
            while (stream.Position < end)
            {
                int b = stream.ReadByte();
                if (b != 10) throw new NotImplementedException();
                obj.vals.Add(OSM.ReadString(stream));
            }
            if (stream.Position != end) throw new NotImplementedException();
            return obj;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.LibraryWrappers.OSM
{
    class PrimitiveGroup
    {
        public List<RawNode> nodes = new List<RawNode>(); // 1
        public List<DenseNodes> dense = new List<DenseNodes>(); // 2 (doc says it should be optional, not a list, but w/e)
        public List<RawWay> ways = new List<RawWay>(); // 3
        public List<Relation> relations = new List<Relation>(); // 4
        public List<ChangeSet> changesets = new List<ChangeSet>(); // 5

        internal static PrimitiveGroup Read(Stream stream)
        {
            PrimitiveGroup obj = new PrimitiveGroup();
            long lengthInBytes = OSM.ReadVarInt(stream);
            long end = stream.Position + lengthInBytes;
            int b = stream.ReadByte();
            while (b == 10 || b == 18 || b == 26 || b == 34 || b == 42)
            {
                if (b == 10)
                {
                    OSM.SkipBytes(stream);
                    if (stream.Position > end) throw new NotImplementedException();
                    if (stream.Position == end) return obj;
                    b = stream.ReadByte();
                }
                else if (b == 18)
                {
                    obj.dense.Add(DenseNodes.Read(stream));
                    if (stream.Position > end) throw new NotImplementedException();
                    if (stream.Position == end) return obj;
                    b = stream.ReadByte();
                }
                else if (b == 26)
                {
                    obj.ways.Add(RawWay.Read(stream));
                    if (stream.Position > end) throw new NotImplementedException();
                    if (stream.Position == end) return obj;
                    b = stream.ReadByte();
                }
                else if (b == 34)
                {
                    OSM.SkipBytes(stream);
                    if (stream.Position > end) throw new NotImplementedException();
                    if (stream.Position == end) return obj;
                    b = stream.ReadByte();
                }
                else if (b == 42)
                {
                    OSM.SkipBytes(stream);
                    if (stream.Position > end) throw new NotImplementedException();
                    if (stream.Position == end) return obj;
                    b = stream.ReadByte();
                }
            }
            throw new NotImplementedException();
        }
    }
}

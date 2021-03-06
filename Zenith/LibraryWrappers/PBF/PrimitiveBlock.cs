﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Zenith.LibraryWrappers.OSM
{
    public class PrimitiveBlock
    {
        public StringTable stringtable; // 1
        public List<PrimitiveGroup> primitivegroup = new List<PrimitiveGroup>(); // 2
        public int granularity = 100; // 17
        public long lat_offset = 0; // 19
        public long lon_offset = 0; // 20
        public int date_granularity = 1000; // 18

        internal static PrimitiveBlock Read(Stream stream)
        {
            PrimitiveBlock obj = new PrimitiveBlock();
            int b = stream.ReadByte();
            if (b != 10) throw new NotImplementedException();
            obj.stringtable = StringTable.Read(stream);
            b = stream.ReadByte();
            while (b == 18)
            {
                obj.primitivegroup.Add(PrimitiveGroup.Read(stream));
                b = stream.ReadByte();
            }
            // NOTE: looks like those OSMSharp broken up files don't have any of these
            if (b == 136)
            {
                obj.granularity = (int)OSMReader.ReadVarInt(stream);
                b = stream.ReadByte();
            }
            if (b == 152)
            {
                obj.lat_offset = OSMReader.ReadVarInt(stream);
                b = stream.ReadByte();
            }
            if (b == 160)
            {
                obj.lon_offset = OSMReader.ReadVarInt(stream);
                b = stream.ReadByte();
            }
            if (b == 144)
            {
                obj.date_granularity = (int)OSMReader.ReadVarInt(stream);
                b = stream.ReadByte();
            }
            if (b != -1) throw new NotImplementedException();
            return obj;
        }
    }
}

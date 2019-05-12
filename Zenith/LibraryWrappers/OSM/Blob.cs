using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    class Blob
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
            List<VertexPositionColor> roads = new List<VertexPositionColor>();
            int highwayIndex = pBlock.stringtable.vals.IndexOf(key);
            int? valueIndex = value == null ? (int?)null : pBlock.stringtable.vals.IndexOf(value);
            foreach (var pGroup in pBlock.primitivegroup)
            {
                foreach (var d in pGroup.dense)
                {
                    if (d.id.Count != d.lat.Count || d.lat.Count != d.lon.Count) throw new NotImplementedException();
                    for (int i = 0; i < d.id.Count; i++)
                    {
                        double longitude = .000000001 * (pBlock.lon_offset + (pBlock.granularity * d.lon[i]));
                        double latitude = .000000001 * (pBlock.lat_offset + (pBlock.granularity * d.lat[i]));
                        info.nodes[d.id[i]] = new Vector2d(longitude * Math.PI / 180, latitude * Math.PI / 180);
                    }
                }
            }
            foreach (var pGroup in pBlock.primitivegroup)
            {
                foreach (var way in pGroup.ways)
                {
                    if (way.keys.Contains(highwayIndex) && (valueIndex == null || way.vals.Contains(valueIndex.Value)))
                    {
                        info.refs.Add(way.refs);
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
            List<VertexPositionColor> roads = new List<VertexPositionColor>();
            foreach (var pGroup in pBlock.primitivegroup)
            {
                foreach (var d in pGroup.dense)
                {
                    if (d.id.Count != d.lat.Count || d.lat.Count != d.lon.Count) throw new NotImplementedException();
                    for (int i = 0; i < d.id.Count; i++)
                    {
                        double longitude = .000000001 * (pBlock.lon_offset + (pBlock.granularity * d.lon[i]));
                        double latitude = .000000001 * (pBlock.lat_offset + (pBlock.granularity * d.lat[i]));
                        info.nodes[d.id[i]] = new Vector2d(longitude * Math.PI / 180, latitude * Math.PI / 180);
                    }
                }
            }
            foreach (var pGroup in pBlock.primitivegroup)
            {
                foreach (var way in pGroup.ways)
                {
                    if (idHash.Contains(way.id))
                    {
                        info.refs.Add(way.refs);
                    }
                }
            }
            return info;
        }

        internal void Init()
        {
            if (type != "OSMData") return;
            using (var memStream = new MemoryStream(zlib_data))
            {
                // skip first two bytes
                // "Those bytes are part of the zlib specification (RFC 1950), not the deflate specification (RFC 1951). Those bytes contain information about the compression method and flags."
                memStream.ReadByte();
                memStream.ReadByte();
                using (var deflateStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    byte[] unzipped = new byte[raw_size];
                    deflateStream.Read(unzipped, 0, raw_size);
                    zlib_data = unzipped;
                    pBlock = PrimitiveBlock.Read(new MemoryStream(zlib_data));
                }
            }
        }

        internal class RoadInfoVector
        {
            public Dictionary<long, Vector2d> nodes = new Dictionary<long, Vector2d>();
            public List<List<long>> refs = new List<List<long>>();
        }
    }
}

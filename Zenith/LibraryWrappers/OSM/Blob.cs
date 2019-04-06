using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        internal RoadInfo GetRoads()
        {
            if (type != "OSMData") return new RoadInfo();
            RoadInfo info = new RoadInfo();
            List<VertexPositionColor> roads = new List<VertexPositionColor>();
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
                    PrimitiveBlock pBlock = PrimitiveBlock.Read(new MemoryStream(zlib_data), "highway");
                    int highwayIndex = pBlock.stringtable.vals.IndexOf("highway");
                    foreach (var pGroup in pBlock.primitivegroup)
                    {
                        foreach (var d in pGroup.dense)
                        {
                            if (d.id.Count != d.lat.Count || d.lat.Count != d.lon.Count) throw new NotImplementedException();
                            for (int i = 0; i < d.id.Count; i++)
                            {
                                double longitude = .000000001 * (pBlock.lon_offset + (pBlock.granularity * d.lon[i]));
                                double latitude = .000000001 * (pBlock.lat_offset + (pBlock.granularity * d.lat[i]));
                                info.nodes[d.id[i]] = new Vector3((float)(longitude * Math.PI / 180), (float)(latitude * Math.PI / 180), -10f);
                            }
                        }
                    }
                    foreach (var pGroup in pBlock.primitivegroup)
                    {
                        foreach (var way in pGroup.ways)
                        {
                            if (way.keys.Contains(highwayIndex))
                            {
                                info.refs.Add(way.refs);
                            }
                        }
                    }
                }
            }
            return info;
        }

        internal List<long> GetDenseNodeStarts()
        {
            var answer = new List<long>();
            if (type != "OSMData") return answer;
            using (var memStream = new MemoryStream(zlib_data))
            {
                memStream.ReadByte();
                memStream.ReadByte();
                using (var deflateStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    byte[] unzipped = new byte[raw_size];
                    deflateStream.Read(unzipped, 0, raw_size);
                    zlib_data = unzipped;
                    PrimitiveBlock pBlock = PrimitiveBlock.ReadDenseNodeStartOnly(new MemoryStream(zlib_data));
                    foreach (var pGroup in pBlock.primitivegroup)
                    {
                        foreach (var d in pGroup.dense) {
                            if (d.id.Count > 0) answer.Add(d.id[0]);
                        }
                    }
                }
            }
            return answer;
        }

        internal void WriteWayIds(FileStream writer, string keyFilter)
        {
            if (type != "OSMData") return;
            using (var memStream = new MemoryStream(zlib_data))
            {
                memStream.ReadByte();
                memStream.ReadByte();
                using (var deflateStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    byte[] unzipped = new byte[raw_size];
                    deflateStream.Read(unzipped, 0, raw_size);
                    zlib_data = unzipped;
                    PrimitiveBlock pBlock = PrimitiveBlock.ReadWayInfoOnly(new MemoryStream(zlib_data), "highway");
                    int highwayIndex = pBlock.stringtable.vals.IndexOf("highway");
                    var bWriter = new BinaryWriter(writer);
                    foreach (var pGroup in pBlock.primitivegroup)
                    {
                        foreach (var way in pGroup.ways)
                        {
                            if (way.keys.Contains(highwayIndex))
                            {
                                bWriter.Write(way.refs.Count);
                                foreach (long id in way.refs) bWriter.Write(id);
                            }
                        }
                    }
                }
            }
        }

        internal List<List<long>> GetWayIds(string keyFilter)
        {
            var answer = new List<List<long>>();
            if (type != "OSMData") return answer;
            using (var memStream = new MemoryStream(zlib_data))
            {
                memStream.ReadByte();
                memStream.ReadByte();
                using (var deflateStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    byte[] unzipped = new byte[raw_size];
                    deflateStream.Read(unzipped, 0, raw_size);
                    zlib_data = unzipped;
                    PrimitiveBlock pBlock = PrimitiveBlock.ReadWayInfoOnly(new MemoryStream(zlib_data), "highway");
                    int highwayIndex = pBlock.stringtable.vals.IndexOf("highway");
                    foreach (var pGroup in pBlock.primitivegroup)
                    {
                        foreach (var way in pGroup.ways)
                        {
                            if (way.keys.Contains(highwayIndex))
                            {
                                answer.Add(way.refs);
                            }
                        }
                    }
                }
            }
            return answer;
        }

        internal class RoadInfo
        {
            public Dictionary<long, Vector3> nodes = new Dictionary<long, Vector3>();
            public List<List<long>> refs = new List<List<long>>();
        }

        internal List<DenseNodes> GetDense()
        {
            var answer = new List<DenseNodes>();
            if (type != "OSMData") return answer;
            using (var memStream = new MemoryStream(zlib_data))
            {
                memStream.ReadByte();
                memStream.ReadByte();
                using (var deflateStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    byte[] unzipped = new byte[raw_size];
                    deflateStream.Read(unzipped, 0, raw_size);
                    zlib_data = unzipped;
                    PrimitiveBlock pBlock = PrimitiveBlock.ReadDenseNodesOnly(new MemoryStream(zlib_data));
                    foreach (var pGroup in pBlock.primitivegroup)
                    {
                        answer.AddRange(pGroup.dense);
                    }
                }
            }
            return answer;
        }
    }
}

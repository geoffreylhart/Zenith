using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OsmSharp;
using OsmSharp.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZMath;
using static Zenith.LibraryWrappers.OSM.Blob;

namespace Zenith.LibraryWrappers.OSM
{
    class OSM
    {
        internal static List<VertexPositionColor> GetRoadsFast(string path)
        {
            List<Blob> blobs = new List<Blob>();
            List<RoadInfo> roads = new List<RoadInfo>();
            using (var reader = File.OpenRead(path))
            {
                while (CanRead(reader))
                {
                    Blob blob = ReadBlob(reader);
                    blobs.Add(blob);
                    roads.Add(blob.GetRoads());
                }
            }
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            foreach (var r in roads)
            {
                foreach (var way in r.refs)
                {
                    VertexPositionColor? prev = null;
                    for (int i = 0; i < way.Count; i++)
                    {
                        VertexPositionColor? v = null;
                        foreach (var r2 in roads)
                        {
                            if (r2.nodes.ContainsKey(way[i]))
                            {
                                v = new VertexPositionColor(r2.nodes[way[i]], Color.White);
                            }
                        }
                        if (prev != null && v != null)
                        {
                            vertices.Add(prev.Value);
                            vertices.Add(v.Value);
                        }
                        prev = v;
                    }
                }
            }
            return vertices;
        }

        internal static List<long> ReadPackedSignedVarInts(Stream stream)
        {
            long length = ReadVarInt(stream);
            long end = stream.Position + length;
            List<long> nums = new List<long>();
            while (stream.Position < end)
            {
                nums.Add(ReadSignedVarInt(stream));
            }
            if (stream.Position != end) throw new NotImplementedException();
            return nums;
        }

        private static long ReadSignedVarInt(Stream reader)
        {
            // note, these varints have the least significant byte first
            // note, the sign bit is the first byte
            int b = reader.ReadByte();
            if ((b & 1) == 1)
            {
                long value = ((b & 127) + 1) / -2;
                long move = -64;
                while ((b & 128) == 128)
                {
                    b = reader.ReadByte();
                    value += (b & 127) * move;
                    move *= 128L;
                }
                return value;
            }
            else
            {
                long value = (b & 127) / 2;
                long move = 64;
                while ((b & 128) == 128)
                {
                    b = reader.ReadByte();
                    value += (b & 127) * move;
                    move *= 128L;
                }
                return value;
            }
        }

        public static long ReadVarInt(Stream reader)
        {
            // note, these varints have the least significant byte first
            long b = reader.ReadByte();
            long value = b & 127;
            int move = 7;
            while ((b & 128) == 128)
            {
                b = reader.ReadByte();
                value += (b & 127) << move;
                move += 7;
            }
            return value;
        }

        internal static List<long> ReadPackedDeltaCodedVarInts(Stream stream)
        {
            long length = ReadVarInt(stream);
            long end = stream.Position + length;
            List<long> nums = new List<long>();
            while (stream.Position < end)
            {
                if (nums.Count == 0)
                {
                    nums.Add(ReadSignedVarInt(stream));
                }
                else
                {
                    nums.Add(nums.Last() + ReadSignedVarInt(stream));
                }
            }
            if (stream.Position != end) throw new NotImplementedException();
            return nums;
        }

        internal static List<long> ReadPackedVarInts(Stream stream)
        {
            long length = ReadVarInt(stream);
            long end = stream.Position + length;
            List<long> nums = new List<long>();
            while (stream.Position < end)
            {
                nums.Add(ReadVarInt(stream));
            }
            if (stream.Position != end) throw new NotImplementedException();
            return nums;
        }

        internal static void SkipBytes(Stream stream)
        {
            long length = ReadVarInt(stream);
            stream.Seek(length, SeekOrigin.Current);
        }

        private static bool CanRead(FileStream reader)
        {
            if (reader.ReadByte() < 0) return false;
            reader.Seek(-1, SeekOrigin.Current);
            return true;
        }

        private static Blob ReadBlob(Stream reader)
        {
            Blob blob = new Blob();
            blob.length = ReadInt32(reader);
            long endOfHead = blob.length + reader.Position;
            byte b = (byte)reader.ReadByte();
            if (b != 10) throw new NotImplementedException();
            blob.type = ReadString(reader);
            b = (byte)reader.ReadByte();
            if (b == 18)
            {
                blob.indexdata = ReadBytes(reader);
                b = (byte)reader.ReadByte();
            }
            if (b != 24) throw new NotImplementedException();
            blob.datasize = (int)ReadVarInt(reader);
            if (reader.Position != endOfHead) throw new NotImplementedException();
            b = (byte)reader.ReadByte();
            if (b == 16)
            {
                blob.raw_size = (int)ReadVarInt(reader);
                b = (byte)reader.ReadByte();
            }
            if (b != 26) throw new NotImplementedException();
            blob.zlib_data = ReadBytes(reader);
            return blob;
        }

        public static byte[] ReadBytes(Stream reader)
        {
            long length = ReadVarInt(reader);
            byte[] bytes = new byte[length];
            reader.Read(bytes, 0, (int)length);
            return bytes;
        }

        public static string ReadString(Stream reader)
        {
            byte[] bytes = ReadBytes(reader);
            char[] chars = new char[bytes.LongLength];
            for (long i = 0; i < bytes.LongLength; i++) chars[i] = (char)bytes[i];
            return new string(chars);
        }

        public static int ReadInt32(Stream reader)
        {
            int b1 = reader.ReadByte() << 12;
            int b2 = reader.ReadByte() << 8;
            int b3 = reader.ReadByte() << 4;
            int b4 = reader.ReadByte();
            return b1 + b2 + b3 + b4;
        }

        internal static List<VertexPositionColor> GetRoadsSlow(string path)
        {
            List<OsmSharp.Node> nodes = new List<OsmSharp.Node>();
            List<OsmSharp.Way> highways = new List<OsmSharp.Way>();
            using (var reader = new FileInfo(path).OpenRead())
            {
                using (var src = new PBFOsmStreamSource(reader))
                {
                    foreach (var element in src)
                    {
                        if (element is OsmSharp.Node) nodes.Add((OsmSharp.Node)element);
                        if (IsHighway(element)) highways.Add((OsmSharp.Way)element);
                    }
                }
            }
            var shapeAsVertices = new List<VertexPositionColor>();
            foreach (var highway in highways)
            {
                for (int i = 0; i < highway.Nodes.Length - 1; i++)
                {
                    OsmSharp.Node node1 = new OsmSharp.Node();
                    OsmSharp.Node node2 = new OsmSharp.Node();
                    node1.Id = highway.Nodes[i];
                    node2.Id = highway.Nodes[i + 1];
                    int found1 = nodes.BinarySearch(node1, new NodeComparer());
                    int found2 = nodes.BinarySearch(node2, new NodeComparer());
                    if (found1 < 0) continue;
                    if (found2 < 0) continue;
                    node1 = nodes[found1];
                    node2 = nodes[found2];
                    LongLat longlat1 = new LongLat(node1.Longitude.Value * Math.PI / 180, node1.Latitude.Value * Math.PI / 180);
                    LongLat longlat2 = new LongLat(node2.Longitude.Value * Math.PI / 180, node2.Latitude.Value * Math.PI / 180);
                    shapeAsVertices.Add(new VertexPositionColor(new Vector3(longlat1, -10f), Microsoft.Xna.Framework.Color.White));
                    shapeAsVertices.Add(new VertexPositionColor(new Vector3(longlat2, -10f), Microsoft.Xna.Framework.Color.White));
                }
            }
            return shapeAsVertices;
        }

        private static bool IsHighway(OsmGeo element)
        {
            if (!(element is OsmSharp.Way)) return false;
            foreach (var tag in element.Tags)
            {
                if (tag.Key == "highway")
                {
                    return true;
                }
            }
            return false;
        }
    }
}

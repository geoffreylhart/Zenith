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
    class Restructurer
    {
        static string WAY_ID_OUTPUT = @"..\..\..\..\LocalCache\ways.txt";
        static string TEMP_FOLDER = @"..\..\..\..\LocalCache\temp";

        internal void JustRewrite(string wayKey, string outputPath)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            // note: there are 1425081696 ways with the highway tag
            long blockPos = 3798679571;
            using (var writer = File.OpenWrite(outputPath))
            {
                BinaryWriter binaryWriter = new BinaryWriter(writer);
                for(int i = 508; i < 1024; i++)
                {
                    for(int j = 341; j < 1024; j++)
                    {
                        Sector sector = new Sector(i, j, 10);
                        List<Blob> blobs = new List<Blob>();
                        List<RoadInfo> roads = new List<RoadInfo>();
                        using (var reader = File.OpenRead(OSMPaths.GetSectorPath(sector)))
                        {
                            while (OSM.CanRead(reader))
                            {
                                Blob blob = OSM.ReadBlob(reader);
                                blobs.Add(blob);
                                roads.Add(blob.GetRoadLongLats());
                            }
                        }
                        List<LongLatPair> vertices = new List<LongLatPair>();
                        foreach (var r in roads)
                        {
                            foreach (var way in r.refs)
                            {
                                LongLatPair prev = null;
                                for (int k = 0; k < way.Count; k++)
                                {
                                    LongLatPair v = null;
                                    foreach (var r2 in roads)
                                    {
                                        if (r2.nodes.ContainsKey(way[k]))
                                        {
                                            v = r2.nodes[way[k]];
                                        }
                                    }
                                    if (prev != null && v != null)
                                    {
                                        vertices.Add(prev);
                                        vertices.Add(v);
                                    }
                                    prev = v;
                                }
                            }
                        }
                        MemoryStream uncompressed = new MemoryStream();
                        OSM.WriteVarInt(uncompressed, vertices.Count);
                        LongLatPair prev2 = new LongLatPair(0, 0);
                        foreach(var vertex in vertices)
                        {
                            OSM.WriteSignedVarInt(uncompressed, vertex.x - prev2.x);
                            OSM.WriteSignedVarInt(uncompressed, vertex.y - prev2.y);
                            prev2 = vertex;
                        }
                        MemoryStream compressed = new MemoryStream();
                        using (var deflateStream = new DeflateStream(compressed, CompressionMode.Compress))
                        {
                            deflateStream.Write(uncompressed.GetBuffer(), 0, uncompressed.GetBuffer().Length);
                        }
                        writer.Seek((i * 1024 + j) * 8, SeekOrigin.Begin);
                        binaryWriter.Write(blockPos);
                        writer.Seek(blockPos, SeekOrigin.Begin);
                        byte[] compressedBytes = compressed.GetBuffer();
                        writer.Write(compressedBytes, 0, compressedBytes.Length);
                        compressed.Dispose();
                        uncompressed.Dispose();
                        blockPos += compressedBytes.Length;
                    }
                }
            }
        }
    }
}

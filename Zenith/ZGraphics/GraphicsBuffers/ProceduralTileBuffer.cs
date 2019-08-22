using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    // TODO: think if this belongs somewhere else or divied up, we're definitely compromising here
    // we're using this class to handle the flow from OSM source to texture
    public class ProceduralTileBuffer : IGraphicsBuffer
    {
        private ISector sector;
        // cache for loaded lines
        LineGraph beachGraph;
        LineGraph lakesGraph;
        LineGraph multiLakesGraph;
        LineGraph roadGraph;
        // cache for VertexLists, right before they get assigned to actual Buffers
        List<VertexPositionColor> beachVertices;
        List<VertexPositionColor> lakeVertices;
        // actual final buffer that gets drawn
        VectorTileBuffer vectorTileBuffer;

        public ProceduralTileBuffer(ISector sector)
        {
            this.sector = sector;
        }

        public void LoadLinesFromFile()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            BlobCollection blobs = OSMReader.GetAllBlobs(sector);
            beachGraph = blobs.GetBeachFast();
            lakesGraph = blobs.GetLakesFast();
            multiLakesGraph = blobs.GetMultiLakesFast();
            roadGraph = blobs.GetRoadsFast();
            Console.WriteLine($"File read for {sector} in {sw.Elapsed.TotalSeconds} s");
        }

        public void GenerateVertices()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            beachVertices = OSMBufferGenerator.GetCoastVertices(beachGraph, sector);
            // TODO: break up beach coast into vertex and buffer
            lakeVertices = OSMBufferGenerator.Tesselate(lakesGraph.ToContours(), Pallete.OCEAN_BLUE);
            lakeVertices.AddRange(OSMBufferGenerator.Tesselate(multiLakesGraph.ToContours(), Pallete.OCEAN_BLUE));
            // TODO: break up lake coasts into vertex and buffer
            // TODO: break up roads into vertex and buffer
            // dereference
            //beachGraph = null;
            //lakesGraph = null;
            //multiLakesGraph = null;
            //roadGraph = null;
            Console.WriteLine($"Vertices generated for {sector} in {sw.Elapsed.TotalSeconds} s");
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            vectorTileBuffer = new VectorTileBuffer(graphicsDevice, sector);
            vectorTileBuffer.Add(graphicsDevice, new BasicVertexBuffer(graphicsDevice, beachVertices, PrimitiveType.TriangleList), sector);
            double widthInFeet = 10.7 * 50; // extra thick
            double circumEarth = 24901 * 5280;
            double width = widthInFeet / circumEarth * 2 * Math.PI;
            vectorTileBuffer.Add(graphicsDevice, beachGraph.ConstructAsRoads(graphicsDevice, width, GlobalContent.BeachFlipped, Microsoft.Xna.Framework.Color.White), sector);
            vectorTileBuffer.Add(graphicsDevice, new BasicVertexBuffer(graphicsDevice, lakeVertices, PrimitiveType.TriangleList), sector);
            lakesGraph.Combine(multiLakesGraph); // TODO: this alters lakesGraph
            vectorTileBuffer.Add(graphicsDevice, lakesGraph.ConstructAsRoads(graphicsDevice, width, GlobalContent.Beach, Microsoft.Xna.Framework.Color.White), sector);
            vectorTileBuffer.Add(graphicsDevice, roadGraph.ConstructAsRoads(graphicsDevice, width * 4 / 50, GlobalContent.Road, Microsoft.Xna.Framework.Color.White), sector);
            //PointCollection points = new PointCollection(sector, 100000); // 3 trillion trees on earth (eh, just guess)
            //roadGraph.Combine(lakesGraph); // TODO: this alters roadGraph
            //points = points.KeepWithin(beachVertices).ExcludeWithin(lakeVertices).RemoveNear(roadGraph, width);
            //vectorTileBuffer.Add(graphicsDevice, points.Construct(graphicsDevice, width, GlobalContent.Tree, sector), sector);
            // dereference
            beachVertices = null;
            lakeVertices = null;
            Console.WriteLine($"Buffers generated for {sector} in {sw.Elapsed.TotalSeconds} s");
        }

        public void Dispose()
        {
            beachGraph = null;
            lakesGraph = null;
            multiLakesGraph = null;
            roadGraph = null;
            beachVertices = null;
            vectorTileBuffer = null;
            if (vectorTileBuffer != null) vectorTileBuffer.Dispose();
            vectorTileBuffer = null;
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            vectorTileBuffer.Draw(renderTarget, minX, maxX, minY, maxY, cameraZoom);
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            return vectorTileBuffer.GetImage(graphicsDevice);
        }

        // return a compressed stream of the preloaded vertex information
        public byte[] GetVerticesBytes()
        {
            using (var memStream = new MemoryStream())
            {
                beachGraph.WriteToStream(memStream);
                lakesGraph.WriteToStream(memStream);
                multiLakesGraph.WriteToStream(memStream);
                roadGraph.WriteToStream(memStream);
                return Deflate(memStream.ToArray(), CompressionMode.Compress);
            }
        }

        public void SetVerticesFromBytes(byte[] bytes)
        {
            using (var memStream = new MemoryStream(Deflate(bytes, CompressionMode.Decompress)))
            {
                beachGraph = new LineGraph().ReadFromStream(memStream);
                lakesGraph = new LineGraph().ReadFromStream(memStream);
                multiLakesGraph = new LineGraph().ReadFromStream(memStream);
                roadGraph = new LineGraph().ReadFromStream(memStream);
            }
        }

        // just an example, right now
        private static byte[] Deflate(byte[] x, CompressionMode mode)
        {
            using (var inStream = new MemoryStream(x))
            using (var outStream = new MemoryStream())
            {
                Deflate(inStream, outStream, mode);
                return outStream.ToArray();
            }
        }

        private static void Deflate(Stream inStream, Stream outStream, CompressionMode mode)
        {
            if (mode == CompressionMode.Compress)
            {
                using (var deflateStream = new DeflateStream(outStream, mode)) inStream.CopyTo(deflateStream);
            }
            else
            {
                using (var deflateStream = new DeflateStream(inStream, mode)) deflateStream.CopyTo(outStream);
            }
        }
    }
}

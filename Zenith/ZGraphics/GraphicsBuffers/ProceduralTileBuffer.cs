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
        // actual final buffers that gets drawn
        VectorTileBuffer vectorTileBuffer;
        TreeBuffer treeBuffer;

        public ProceduralTileBuffer(ISector sector)
        {
            this.sector = sector;
        }

        public void LoadLinesFromFile()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            BlobCollection blobs = OSMReader.GetAllBlobs(sector);
            Console.WriteLine($"Blobs read for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            beachGraph = blobs.GetBeachFast();
            Console.WriteLine($"Beach graph generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            lakesGraph = blobs.GetLakesFast();
            Console.WriteLine($"Lakes graph generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            multiLakesGraph = blobs.GetMultiLakesFast();
            Console.WriteLine($"Multilakes graph generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            roadGraph = blobs.GetRoadsFast();
            Console.WriteLine($"Road graph generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
        }

        public void GenerateVertices()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            beachVertices = OSMBufferGenerator.GetCoastVertices(beachGraph, sector);
            Console.WriteLine($"Beach verticies generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            // TODO: break up beach coast into vertex and buffer
            lakeVertices = OSMBufferGenerator.Tesselate(lakesGraph.ToContours(), Pallete.OCEAN_BLUE);
            Console.WriteLine($"Lake verticies generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            var multiLakeVertices = OSMBufferGenerator.Tesselate(multiLakesGraph.ToContours(), Pallete.OCEAN_BLUE);
            Console.WriteLine($"Multilake vertices generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            lakeVertices.AddRange(multiLakeVertices);
            Console.WriteLine($"Lake and multilake vertices combined for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            // TODO: break up lake coasts into vertex and buffer
            // TODO: break up roads into vertex and buffer
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            vectorTileBuffer = new VectorTileBuffer(graphicsDevice, sector);
            BasicVertexBuffer beachBuffer = new BasicVertexBuffer(graphicsDevice, beachVertices, PrimitiveType.TriangleList);
            vectorTileBuffer.Add(graphicsDevice, beachBuffer, sector);
            Console.WriteLine($"Beach buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            double widthInFeet = 10.7 * 50; // extra thick
            double circumEarth = 24901 * 5280;
            double width = widthInFeet / circumEarth * 2 * Math.PI;
            vectorTileBuffer.Add(graphicsDevice, beachGraph.ConstructAsRoads(graphicsDevice, width, GlobalContent.BeachFlipped, Microsoft.Xna.Framework.Color.White), sector);
            Console.WriteLine($"Beach coast buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            BasicVertexBuffer lakesBuffer = new BasicVertexBuffer(graphicsDevice, lakeVertices, PrimitiveType.TriangleList);
            vectorTileBuffer.Add(graphicsDevice, lakesBuffer, sector);
            Console.WriteLine($"Lakes and multilakes buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            lakesGraph.Combine(multiLakesGraph); // TODO: this alters lakesGraph
            Console.WriteLine($"Lakes and multilakes graph combined for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            vectorTileBuffer.Add(graphicsDevice, lakesGraph.ConstructAsRoads(graphicsDevice, width, GlobalContent.Beach, Microsoft.Xna.Framework.Color.White), sector);
            Console.WriteLine($"Lakes and multilakes coast buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            BasicVertexBuffer roadsBuffer = roadGraph.ConstructAsRoads(graphicsDevice, width * 4 / 50, GlobalContent.Road, Microsoft.Xna.Framework.Color.White);
            vectorTileBuffer.Add(graphicsDevice, roadsBuffer, sector);
            Console.WriteLine($"Roads buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            treeBuffer = new TreeBuffer(graphicsDevice, beachBuffer, lakesBuffer, roadsBuffer, sector);
            Console.WriteLine($"Trees generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            // dereference
            beachVertices = null;
            lakeVertices = null;
        }

        public void Dispose()
        {
            beachGraph = null;
            lakesGraph = null;
            multiLakesGraph = null;
            roadGraph = null;
            beachVertices = null;
            if (vectorTileBuffer != null) vectorTileBuffer.Dispose();
            vectorTileBuffer = null;
            if (treeBuffer != null) treeBuffer.Dispose();
            treeBuffer = null;
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            vectorTileBuffer.Draw(renderTarget, minX, maxX, minY, maxY, cameraZoom);
            treeBuffer.Draw(renderTarget, minX, maxX, minY, maxY, cameraZoom);
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

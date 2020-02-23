using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers;
using Zenith.LibraryWrappers.OSM;
using Zenith.MathHelpers;
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
        SectorConstrainedAreaMap landAreaMap;
        LineGraph roadGraph;
        // actual final buffers that gets drawn
        VectorTileBuffer vectorTileBuffer;
        VectorTileBuffer vectorTileBufferUnused;
        TreeGeometryBuffer treeBuffer;
        HouseBuffer houseBuffer;
        DebugBuffer debugBuffer;

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
            BlobsIntersector.DoIntersections(blobs);
            landAreaMap = blobs.GetCoastAreaMap("natural", "coastline").Subtract(blobs.GetAreaMap("natural", "water"), blobs, false).Finalize(blobs);
            Console.WriteLine($"Land area map generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            roadGraph = blobs.GetRoadsFast();
            Console.WriteLine($"Road graph generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
        }

        public void GenerateVertices()
        {
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
            double widthInFeet = 10.7 * 50; // extra thick
            double circumEarth = 24901 * 5280;
            double width = widthInFeet / circumEarth * 2 * Math.PI;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            vectorTileBuffer = new VectorTileBuffer(graphicsDevice, sector);
            vectorTileBufferUnused = new VectorTileBuffer(graphicsDevice, sector);
            BasicVertexBuffer landBuffer = landAreaMap.Tesselate(graphicsDevice, Pallete.GRASS_GREEN);
            vectorTileBuffer.Add(graphicsDevice, landBuffer, sector);
            Console.WriteLine($"Land buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            BasicVertexBuffer coastBuffer = landAreaMap.ConstructAsRoads(graphicsDevice, width/10.0, GlobalContent.Beach, Color.White);
            vectorTileBuffer.Add(graphicsDevice, coastBuffer, sector);
            BasicVertexBuffer coastBufferFat = landAreaMap.ConstructAsRoads(graphicsDevice, width * 2/10.0, GlobalContent.Beach, Color.White);
            vectorTileBufferUnused.Add(graphicsDevice, coastBufferFat, sector);
            Console.WriteLine($"Coast buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            BasicVertexBuffer roadsBuffer = roadGraph.ConstructAsRoads(graphicsDevice, width * 4 / 50, GlobalContent.Road, Microsoft.Xna.Framework.Color.White);
            BasicVertexBuffer roadsBufferFat = roadGraph.ConstructAsRoads(graphicsDevice, width * 12 / 50, GlobalContent.Road, Microsoft.Xna.Framework.Color.White);
            vectorTileBuffer.Add(graphicsDevice, roadsBuffer, sector);
            vectorTileBufferUnused.Add(graphicsDevice, roadsBufferFat, sector);
            Console.WriteLine($"Roads buffer generated for {sector} in {sw.Elapsed.TotalSeconds} s");
            sw.Restart();
            treeBuffer = new TreeGeometryBuffer(graphicsDevice, coastBuffer, roadsBuffer, roadsBufferFat, landBuffer, sector);
            Console.WriteLine($"Trees generated for {sector} in {sw.Elapsed.TotalSeconds} s");
#if WINDOWS
            sw.Restart();
            List<Matrix> matrices = roadGraph.ConstructHousePositions();
            houseBuffer = new HouseBuffer(graphicsDevice, matrices, sector);
            Console.WriteLine($"Houses generated for {sector} in {sw.Elapsed.TotalSeconds} s");
#endif
            sw.Restart();
            debugBuffer = new DebugBuffer(graphicsDevice, sector);
        }

        public void Dispose()
        {
            landAreaMap = null;
            roadGraph = null;
            if (vectorTileBuffer != null) vectorTileBuffer.Dispose();
            if (vectorTileBufferUnused != null) vectorTileBufferUnused.Dispose();
            vectorTileBuffer = null;
            if (treeBuffer != null) treeBuffer.Dispose();
            treeBuffer = null;
            if (houseBuffer != null) houseBuffer.Dispose();
            houseBuffer = null;
            if (debugBuffer != null) debugBuffer.Dispose();
            debugBuffer = null;
        }

        public void InitDraw(RenderContext context)
        {
            vectorTileBuffer.InitDraw(context);
            treeBuffer.InitDraw(context);
        }

        public void Draw(RenderContext context)
        {
            vectorTileBuffer.Draw(context);
#if WINDOWS
            houseBuffer.Draw(context);
#endif
            treeBuffer.Draw(context);
            if (Game1.DEBUGGING)
            {
                context.graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, context.graphicsDevice.Viewport.MaxDepth, 0);
                debugBuffer.Draw(context);
            }
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            Vector2d topLeft = new Vector2d(0, 0);
            Vector2d bottomRight = new Vector2d(1, 1);
            Matrixd projection = Matrixd.CreateOrthographicOffCenter(0, 1, Math.Sqrt(0.5), 0, -2, 2); // TODO: why negative?
            //projection = Matrixd.CreateOrthographicOffCenter(0.1, 0.105, 0.1 + Math.Sqrt(0.5) * 0.005, 0.1, -2, 2); // TODO: why negative?
            Matrixd skew = Matrixd.CreateRotationX(Math.PI / 4);
            RenderContext context = new RenderContext(graphicsDevice, skew * projection, topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y, 0, RenderContext.LayerPass.MAIN_PASS);
            context.highQuality = true;
            context.deferred = false;
            context.treeExtraPH = Math.Sqrt(0.5); // undo our stretching when measuring tree height (TODO: very hacky)
            InitDraw(context);
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512 * 16, 512 * 16, true, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            RenderTarget2D newGrass = new RenderTarget2D(graphicsDevice, 512 * 16, 512 * 16, true, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            RenderTarget2D newTree = new RenderTarget2D(graphicsDevice, 512 * 16, 512 * 16, true, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            context.treeLayer = newTree;
            context.grassLayer = newGrass;
            graphicsDevice.SetRenderTarget(newTree);
            context.layerPass = RenderContext.LayerPass.TREE_DENSITY_PASS;
            Draw(context);
            graphicsDevice.SetRenderTarget(newGrass);
            context.layerPass = RenderContext.LayerPass.GRASS_DENSITY_PASS;
            Draw(context);
            graphicsDevice.SetRenderTarget(newTarget);
            context.layerPass = RenderContext.LayerPass.MAIN_PASS;
            Draw(context);
            newTree.Dispose();
            newGrass.Dispose();
            return DownScale(graphicsDevice, newTarget, 512);
        }

        private Texture2D DownScale(GraphicsDevice graphicsDevice, RenderTarget2D texture, int newsize)
        {
            Texture2D newtexture = new RenderTarget2D(graphicsDevice, newsize, newsize, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget((RenderTarget2D)newtexture);
            GraphicsBasic.DrawSpriteRect(graphicsDevice, 0, 0, newsize, newsize, texture, BlendState.AlphaBlend, Microsoft.Xna.Framework.Color.White);
            texture.Dispose();
            return newtexture;
        }
    }
}

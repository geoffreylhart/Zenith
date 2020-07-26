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
using Zenith.ZGraphics.Procedural;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    // TODO: think if this belongs somewhere else or divied up, we're definitely compromising here
    // we're using this class to handle the flow from OSM source to texture
    public class ProceduralTileBuffer : IGraphicsBuffer
    {
        private ISector sector;
        // descriptors
        private List<IDescriptor> descriptors = new List<IDescriptor>();
        private List<IDescriptor> treeDescriptors = new List<IDescriptor>();
        private List<IDescriptor> grassDescriptors = new List<IDescriptor>();
        // actual final buffers that gets drawn
        TreeGeometryBuffer treeBuffer;
        DebugBuffer debugBuffer;
        BasicVertexBuffer waterBuffer;

        public ProceduralTileBuffer(ISector sector)
        {
            this.sector = sector;
            var landSource = new SubtractionPolygonSource(new RawPolygonSource("natural", "coastline", true), new RawPolygonSource("natural", "water", false));
            var coastSource = new EdgeLineSource(landSource, true);
            var roadSource = new RawLineSource("highway", null);
            var buildingSource = new RawPolygonSource("building", "yes", false);

            descriptors.Add(new DepthClearDescriptor());
            descriptors.Add(new PolygonRenderDescriptor(landSource, Pallete.GRASS_GREEN));
            descriptors.Add(new LineRenderDescriptor(coastSource, 10.7 * 25 * 256, GlobalContent.Beach, true));
            descriptors.Add(new DepthClearDescriptor());
            descriptors.Add(new LineRenderDescriptor(roadSource, 10.7 * 2 * 256, GlobalContent.Road));
            descriptors.Add(new DepthClearDescriptor());
            descriptors.Add(new BuildingRenderDescriptor(buildingSource, 10.7 * 10, Color.LightGray, GlobalContent.BuildingWall));

            treeDescriptors.Add(new DepthClearDescriptor());
            treeDescriptors.Add(new PolygonRenderDescriptor(landSource, Color.White));
            treeDescriptors.Add(new LineRenderDescriptor(coastSource, 10.7 * 25 * 256, GlobalContent.BeachTreeDensity, true));
            treeDescriptors.Add(new DepthClearDescriptor());
            treeDescriptors.Add(new LineRenderDescriptor(roadSource, 10.7 * 12 * 256, GlobalContent.RoadTreeDensity));
            treeDescriptors.Add(new DepthClearDescriptor());
            treeDescriptors.Add(new LineRenderDescriptor(roadSource, 10.7 * 2 * 256, Color.Black));

            grassDescriptors.Add(new DepthClearDescriptor());
            grassDescriptors.Add(new PolygonRenderDescriptor(landSource, Color.White));
            grassDescriptors.Add(new LineRenderDescriptor(coastSource, 10.7 * 25 * 256, GlobalContent.BeachGrassDensity, true));
            grassDescriptors.Add(new DepthClearDescriptor());
            grassDescriptors.Add(new LineRenderDescriptor(roadSource, 10.7 * 2 * 256, Color.Black));
        }

        public void LoadLinesFromFile()
        {
            BlobCollection blobs = OSMReader.GetAllBlobs(sector);
            BlobsIntersector.DoIntersections(blobs);

            var allDescriptors = new List<IDescriptor>();
            allDescriptors.AddRange(descriptors);
            allDescriptors.AddRange(treeDescriptors);
            allDescriptors.AddRange(grassDescriptors);
            foreach (var descriptor in allDescriptors)
            {
                descriptor.Load(blobs);
                descriptor.Init(blobs);
            }
        }

        public void GenerateVertices()
        {
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
            var allDescriptors = new List<IDescriptor>();
            allDescriptors.AddRange(descriptors);
            allDescriptors.AddRange(treeDescriptors);
            allDescriptors.AddRange(grassDescriptors);
            foreach (var descriptor in allDescriptors)
            {
                descriptor.GenerateBuffers(graphicsDevice);
            }
            treeBuffer = new TreeGeometryBuffer(graphicsDevice);
            debugBuffer = new DebugBuffer(graphicsDevice, sector);
            waterBuffer = GenerateWaterBuffer(graphicsDevice);
        }

        public void Dispose()
        {
            var allDescriptors = new List<IDescriptor>();
            allDescriptors.AddRange(descriptors);
            allDescriptors.AddRange(treeDescriptors);
            allDescriptors.AddRange(grassDescriptors);
            foreach (var descriptor in allDescriptors)
            {
                descriptor.Dispose();
            }
            if (treeBuffer != null) treeBuffer.Dispose();
            if (debugBuffer != null) debugBuffer.Dispose();
            if (waterBuffer != null) waterBuffer.Dispose();
        }

        // sure
        private BasicVertexBuffer GenerateWaterBuffer(GraphicsDevice graphicsDevice)
        {
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            Vector2d topLeft = new Vector2d(0, 0);
            Vector2d topRight = new Vector2d(1, 0);
            Vector2d bottomLeft = new Vector2d(0, 1);
            Vector2d bottomRight = new Vector2d(1, 1);
            vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)topRight.X, (float)topRight.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomLeft.X, (float)bottomLeft.Y, 0), Pallete.OCEAN_BLUE));
            return new BasicVertexBuffer(graphicsDevice, vertices, PrimitiveType.TriangleList);
        }

        public void InitDraw(RenderContext context)
        {
            var allDescriptors = new List<IDescriptor>();
            allDescriptors.AddRange(descriptors);
            allDescriptors.AddRange(treeDescriptors);
            allDescriptors.AddRange(grassDescriptors);
            foreach (var descriptor in allDescriptors)
            {
                descriptor.InitDraw(context);
            }
            treeBuffer.InitDraw(context);
        }

        public void Draw(RenderContext context)
        {
            if (context.layerPass == RenderContext.LayerPass.MAIN_PASS)
            {
                waterBuffer.Draw(context);
                foreach (var descriptor in descriptors)
                {
                    descriptor.Draw(context);
                }
            }
            if (context.highQuality || (context.maxX - context.minX < 0.1 && context.maxY - context.minY < 0.1))
            {
                if (context.layerPass == RenderContext.LayerPass.TREE_DENSITY_PASS)
                {
                    waterBuffer.Draw(context, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
                    foreach (var descriptor in treeDescriptors)
                    {
                        descriptor.Draw(context);
                    }
                }
                if (context.layerPass == RenderContext.LayerPass.GRASS_DENSITY_PASS)
                {
                    waterBuffer.Draw(context, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
                    foreach (var descriptor in grassDescriptors)
                    {
                        descriptor.Draw(context);
                    }
                }
                treeBuffer.Draw(context);
            }
            if (Game1.DEBUGGING)
            {
                context.graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, context.graphicsDevice.Viewport.MaxDepth, 0);
                debugBuffer.Draw(context);
            }
        }


        public static RenderContext context;
        public static RenderTarget2D RENDER_BUFFER;
        public static RenderTarget2D TREE_DENSITY_BUFFER;
        public static RenderTarget2D GRASS_DENSITY_BUFFER;
        public static RenderTarget2D DOWNSAMPLE_BUFFER;

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            if (RENDER_BUFFER == null)
            {
                Vector2d topLeft = new Vector2d(0, 0);
                Vector2d bottomRight = new Vector2d(1, 1);
                Matrixd projection = Matrixd.CreateOrthographicOffCenter(0, 1, Math.Sqrt(0.5), 0, -2, 2); // TODO: why negative?
                                                                                                          //projection = Matrixd.CreateOrthographicOffCenter(0.1, 0.105, 0.1 + Math.Sqrt(0.5) * 0.005, 0.1, -2, 2); // TODO: why negative?
                Matrixd skew = Matrixd.CreateRotationX(Math.PI / 4);
                context = new RenderContext(graphicsDevice, skew * projection, topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y, 0, RenderContext.LayerPass.MAIN_PASS);
                context.highQuality = true;
                context.deferred = false;
                context.treeExtraPH = Math.Sqrt(0.5); // undo our stretching when measuring tree height (TODO: very hacky)
                context.treeLayer = MakeDefaultRenderTarget(graphicsDevice);
                context.grassLayer = MakeDefaultRenderTarget(graphicsDevice);
                RENDER_BUFFER = MakeDefaultRenderTarget(graphicsDevice);
                TREE_DENSITY_BUFFER = context.treeLayer;
                GRASS_DENSITY_BUFFER = context.grassLayer;
            }
            InitDraw(context);
            graphicsDevice.SetRenderTarget(TREE_DENSITY_BUFFER);
            context.layerPass = RenderContext.LayerPass.TREE_DENSITY_PASS;
            Draw(context);
            graphicsDevice.SetRenderTarget(GRASS_DENSITY_BUFFER);
            context.layerPass = RenderContext.LayerPass.GRASS_DENSITY_PASS;
            Draw(context);
            graphicsDevice.SetRenderTarget(RENDER_BUFFER);
            context.layerPass = RenderContext.LayerPass.MAIN_PASS;
            Draw(context);
            return DownScale(graphicsDevice, RENDER_BUFFER, 512);
        }

        private RenderTarget2D MakeDefaultRenderTarget(GraphicsDevice graphicsDevice)
        {
            return new RenderTarget2D(graphicsDevice, 512 * 16, 512 * 16, true, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
        }

        private Texture2D DownScale(GraphicsDevice graphicsDevice, RenderTarget2D texture, int newsize)
        {
            Texture2D newtexture = new RenderTarget2D(graphicsDevice, newsize, newsize, true, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget((RenderTarget2D)newtexture);
            GraphicsBasic.DrawSpriteRect(graphicsDevice, 0, 0, newsize, newsize, texture, BlendState.AlphaBlend, Microsoft.Xna.Framework.Color.White);
            return newtexture;
        }
    }
}

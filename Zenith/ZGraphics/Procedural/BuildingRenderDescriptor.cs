using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;
using Zenith.MathHelpers;
using Zenith.ZGraphics.Procedural;
using Zenith.ZMath;

namespace Zenith.ZGraphics.Procedural
{
    class BuildingRenderDescriptor : IDescriptor
    {
        private IPolygonSource polygonSource;
        private ILineSource lineSource;
        private double heightInFeet;
        private Color roofColor;
        private Texture2D wallTexture;
        private BasicVertexBuffer buffer;
        private BasicVertexBuffer buffer2;

        public BuildingRenderDescriptor(IPolygonSource polygonSource, double heightInFeet, Color roofColor, Texture2D wallTexture)
        {
            this.polygonSource = polygonSource;
            this.lineSource = new EdgeLineSource(polygonSource, true);
            this.heightInFeet = heightInFeet;
            this.roofColor = roofColor;
            this.wallTexture = wallTexture;
        }

        public void Load(BlobCollection blobs)
        {
            polygonSource.Load(blobs);
            lineSource.Load(blobs);
        }

        public void Init(BlobCollection blobs)
        {
            polygonSource.Init(blobs);
            lineSource.Init(blobs);
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
            double height = heightInFeet / 300000;
            buffer = polygonSource.GetMap().Tesselate(graphicsDevice, Color.White);
            buffer2 = lineSource.GetLineGraph().ConstructViaExtrusion(graphicsDevice, new[] { new Vector2d(0, 0), new Vector2d(0, height) }, null, Color.White);
        }

        public void InitDraw(RenderContext context)
        {
        }

        public void Draw(RenderContext context)
        {
            double height = heightInFeet / 300000;
            RenderContext context2 = new RenderContext(context.graphicsDevice, Matrixd.CreateTranslation(new Vector3d(0, 0, height)) * context.WVP, context.minX, context.maxX, context.minY, context.maxY, context.cameraZoom, context.layerPass);
            buffer.Draw(context2, PrimitiveType.TriangleList, null, roofColor.ToVector3());
            buffer2.Draw(context, PrimitiveType.TriangleList, wallTexture, null);
        }

        public void Dispose()
        {
            if (polygonSource != null) polygonSource.Dispose();
            if (buffer != null) buffer.Dispose();
            if (buffer2 != null) buffer2.Dispose();
        }
    }
}

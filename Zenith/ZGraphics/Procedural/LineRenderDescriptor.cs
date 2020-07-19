using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGraphics.GraphicsBuffers;

namespace Zenith.ZGraphics.Procedural
{
    class LineRenderDescriptor : IDescriptor
    {
        private ILineSource lineSource;
        private double widthInFeet;
        private Texture2D texture;
        private Color color;
        private BasicVertexBuffer buffer;

        public LineRenderDescriptor(ILineSource lineSource, double widthInFeet, Texture2D texture)
        {
            this.lineSource = lineSource;
            this.widthInFeet = widthInFeet;
            this.texture = texture;
        }

        public LineRenderDescriptor(ILineSource lineSource, double widthInFeet, Color color)
        {
            this.lineSource = lineSource;
            this.widthInFeet = widthInFeet;
            this.color = color;
        }

        public void Load(BlobCollection blobs)
        {
            lineSource.Load(blobs);
        }

        public void Init(BlobCollection blobs)
        {
            lineSource.Init(blobs);
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
            buffer = lineSource.ConstructAsRoads(graphicsDevice, widthInFeet);
        }

        public void InitDraw(RenderContext context)
        {
        }

        public void Draw(RenderContext context)
        {
            buffer.Draw(context, PrimitiveType.TriangleList, texture, color == null ? (Vector3?)null : color.ToVector3());
        }

        public void Dispose()
        {
            if (lineSource != null) lineSource.Dispose();
            if (buffer != null) buffer.Dispose();
        }
    }
}

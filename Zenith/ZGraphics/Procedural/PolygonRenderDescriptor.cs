using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;

namespace Zenith.ZGraphics.Procedural
{
    class PolygonRenderDescriptor : IDescriptor
    {
        private IPolygonSource polygonSource;
        private Color color;
        private BasicVertexBuffer buffer;

        public PolygonRenderDescriptor(IPolygonSource polygonSource, Color color)
        {
            this.polygonSource = polygonSource;
            this.color = color;
        }

        public void Load(BlobCollection blobs)
        {
            polygonSource.Load(blobs);
        }

        public void Init(BlobCollection blobs)
        {
            polygonSource.Init(blobs);
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
            buffer = polygonSource.GetMap().Tesselate(graphicsDevice, Color.White);
        }

        public void InitDraw(RenderContext context)
        {
        }

        public void Draw(RenderContext context)
        {
            buffer.Draw(context, PrimitiveType.TriangleList, null, color.ToVector3());
        }

        public void Dispose()
        {
            if (polygonSource != null) polygonSource.Dispose();
            if (buffer != null) buffer.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;

namespace Zenith.ZGraphics.Procedural
{
    class DepthClearDescriptor : IDescriptor
    {
        public void Load(BlobCollection blobs)
        {
        }

        public void Init(BlobCollection blobs)
        {
        }

        public void GenerateBuffers(GraphicsDevice graphicsDevice)
        {
        }

        public void InitDraw(RenderContext context)
        {
        }

        public void Draw(RenderContext context)
        {
            context.graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, context.graphicsDevice.Viewport.MaxDepth, 0);
        }

        public void Dispose()
        {
        }
    }
}

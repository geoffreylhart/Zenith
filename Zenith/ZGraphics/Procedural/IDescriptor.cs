using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;

namespace Zenith.ZGraphics.Procedural
{
    interface IDescriptor
    {
        void Load(BlobCollection blobs);
        void Init(BlobCollection blobs);
        void GenerateBuffers(GraphicsDevice graphicsDevice);
        void InitDraw(RenderContext context);
        void Draw(RenderContext context);
        void Dispose();
    }
}

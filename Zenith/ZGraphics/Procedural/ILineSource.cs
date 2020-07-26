using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;

namespace Zenith.ZGraphics.Procedural
{
    interface ILineSource
    {
        LineGraph GetLineGraph();
        void Load(BlobCollection blobs); // necessary to be used at all
        void Init(BlobCollection blobs); // necessary to be rendered
        BasicVertexBuffer ConstructAsRoads(GraphicsDevice graphicsDevice, double widthInFeet, bool outerOnly);
        void Dispose();
    }
}

﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;

namespace Zenith.ZGraphics.Procedural
{
    interface ILineSource
    {
        void Load(BlobCollection blobs); // necessary to be used at all
        void Init(BlobCollection blobs); // necessary to be rendered
        BasicVertexBuffer ConstructAsRoads(GraphicsDevice graphicsDevice, double widthInFeet);
        void Dispose();
    }
}

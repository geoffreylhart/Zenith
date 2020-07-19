using System;
using System.Collections.Generic;
using System.Text;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;

namespace Zenith.ZGraphics.Procedural
{
    interface IPolygonSource
    {
        SectorConstrainedOSMAreaGraph GetGraph();
        SectorConstrainedAreaMap GetMap();
        void Load(BlobCollection blobs); // necessary to be used at all
        void Init(BlobCollection blobs); // necessary to be rendered
        void Dispose();
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;
using Zenith.ZGraphics.GraphicsBuffers;

namespace Zenith.ZGraphics.Procedural
{
    class SubtractionPolygonSource : IPolygonSource
    {
        private bool loaded = false;
        private bool initiated = false;
        private IPolygonSource polygonSource1;
        private IPolygonSource polygonSource2;
        private SectorConstrainedOSMAreaGraph graph;
        private SectorConstrainedAreaMap map;

        public SubtractionPolygonSource(IPolygonSource polygonSource1, IPolygonSource polygonSource2)
        {
            this.polygonSource1 = polygonSource1;
            this.polygonSource2 = polygonSource2;
        }

        public SectorConstrainedOSMAreaGraph GetGraph()
        {
            return graph;
        }

        public SectorConstrainedAreaMap GetMap()
        {
            return map;
        }

        public void Load(BlobCollection blobs)
        {
            if (loaded) return;
            polygonSource1.Load(blobs);
            polygonSource2.Load(blobs);
            graph = polygonSource1.GetGraph().Subtract(polygonSource2.GetGraph(), blobs);
            if (Constants.DEBUG_MODE) graph.CheckValid();
            loaded = true;
        }

        public void Init(BlobCollection blobs)
        {
            if (initiated) return;
            map = graph.Finalize(blobs);
            initiated = true;
        }

        public void Dispose()
        {
            if (polygonSource1 != null) polygonSource1.Dispose();
            if (polygonSource2 != null) polygonSource2.Dispose();
            graph = null;
            map = null;
        }
    }
}

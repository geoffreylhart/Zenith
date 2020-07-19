using System;
using System.Collections.Generic;
using System.Text;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;

namespace Zenith.ZGraphics.Procedural
{
    class RawPolygonSource : IPolygonSource
    {
        private bool loaded = false;
        private bool initiated = false;
        private string key;
        private string value;
        private bool isCoast;
        private SectorConstrainedOSMAreaGraph graph;
        private SectorConstrainedAreaMap map;

        public RawPolygonSource(string key, string value, bool isCoast)
        {
            this.key = key;
            this.value = value;
            this.isCoast = isCoast;
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
            graph = isCoast ? blobs.GetCoastAreaMap(key, value) : blobs.GetAreaMap(key, value);
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
            graph = null;
            map = null;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;
using Zenith.ZGraphics.Procedural;

namespace Zenith.ZGraphics.Procedural
{
    class EdgeLineSource : ILineSource
    {
        private bool loaded = false;
        private bool initiated = false;
        private IPolygonSource polygonSource;
        private bool isCCW;
        private SectorConstrainedOSMAreaGraph graph;
        private LineGraph lineGraph;
        private Dictionary<double, BasicVertexBuffer> bufferCache = new Dictionary<double, BasicVertexBuffer>();

        public EdgeLineSource(IPolygonSource polygonSource, bool isCCW)
        {
            this.polygonSource = polygonSource;
            this.isCCW = isCCW;
            if (!isCCW) throw new NotImplementedException();
        }

        public LineGraph GetLineGraph()
        {
            return lineGraph;
        }

        public void Load(BlobCollection blobs)
        {
            if (loaded) return;
            polygonSource.Load(blobs);
            graph = polygonSource.GetGraph();
            loaded = true;
        }

        public void Init(BlobCollection blobs)
        {
            if (initiated) return;
            lineGraph = graph.Finalize(blobs).ToLineGraph();
            initiated = true;
        }

        public BasicVertexBuffer ConstructAsRoads(GraphicsDevice graphicsDevice, double widthInFeet, bool outerOnly)
        {
            double circumEarth = 24901 * 5280;
            double width = widthInFeet / circumEarth * 2 * Math.PI;
            double key = widthInFeet * (outerOnly ? -1 : 1);
            if (!bufferCache.ContainsKey(key))
            {
                bufferCache[key] = lineGraph.ConstructAsRoads(graphicsDevice, width, null, Color.White, outerOnly);
            }
            return bufferCache[key];
        }

        public void Dispose()
        {
            if (polygonSource != null) polygonSource.Dispose();
            graph = null;
            lineGraph = null;
            foreach (var pair in bufferCache)
            {
                pair.Value.Dispose();
            }
        }
    }
}

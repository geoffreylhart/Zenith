﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;

namespace Zenith.ZGraphics.Procedural
{
    class RawLineSource : ILineSource
    {
        private bool loaded = false;
        private bool initiated = false;
        private string key;
        private string value;
        private LineGraph lineGraph;
        private Dictionary<double, BasicVertexBuffer> bufferCache = new Dictionary<double, BasicVertexBuffer>();

        public RawLineSource(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public LineGraph GetLineGraph()
        {
            return lineGraph;
        }

        public void Load(BlobCollection blobs)
        {
            if (loaded) return;
            lineGraph = blobs.GetFast(key, value);
            loaded = true;
        }

        public void Init(BlobCollection blobs)
        {
            if (initiated) return;
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
            lineGraph = null;
            foreach (var pair in bufferCache)
            {
                pair.Value.Dispose();
            }
        }
    }
}

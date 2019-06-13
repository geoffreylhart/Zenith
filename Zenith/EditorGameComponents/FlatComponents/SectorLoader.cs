﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZGraphics;
using Zenith.ZGraphics.GraphicsBuffers;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    abstract class SectorLoader : IFlatComponent, IEditorGameComponent
    {
        private static int MAX_ZOOM = 19;
        Dictionary<MercatorSector, IGraphicsBuffer> loadedMaps = new Dictionary<MercatorSector, IGraphicsBuffer>();
        HashSet<MercatorSector> toLoad = new HashSet<MercatorSector>();
        MercatorSector previewSquare = null;

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            // autoload stuff
            // TODO: move to update step?
            int zoomLevel = (int)(Math.Log((maxX - minX) / (2 * Math.PI)) / Math.Log(0.5));
            MercatorSector bottomLeft = GetSector(minX, minY, zoomLevel);
            MercatorSector bottomRight = GetSector(maxX, minY, zoomLevel);
            MercatorSector topLeft = GetSector(minX, maxY, zoomLevel);
            MercatorSector topRight = GetSector(maxX, maxY, zoomLevel);
            List<MercatorSector> containedSectors = new List<MercatorSector>();
            for (int i = bottomLeft.X; i <= bottomRight.X; i++)
            {
                for (int j = bottomLeft.Y; j <= topLeft.Y; j++)
                {
                    containedSectors.Add(new MercatorSector(i % (1 << bottomLeft.Zoom), j % (1 << bottomLeft.Zoom), bottomLeft.Zoom));
                }
            }
            List<MercatorSector> unload = new List<MercatorSector>();
            foreach (var pair in loadedMaps)
            {
                if (!containedSectors.Contains(pair.Key))
                {
                    unload.Add(pair.Key);
                }
            }
            foreach (var u in unload)
            {
                if (!AllowUnload(u)) continue;
                loadedMaps[u].Dispose();
                loadedMaps.Remove(u);
            }
            foreach (var c in containedSectors)
            {
                if (DoAutoLoad(c)) toLoad.Add(c);
            }
            // end autoload stuff
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            foreach (var l in toLoad)
            {
                if (!loadedMaps.ContainsKey(l))
                {
                    loadedMaps[l] = GetGraphicsBuffer(renderTarget.GraphicsDevice, l);
                }
            }
            toLoad = new HashSet<MercatorSector>();
            List<MercatorSector> sorted = loadedMaps.Keys.ToList();
            sorted.Sort((x, y) => x.Zoom.CompareTo(y.Zoom));
            foreach (var sector in sorted)
            {
                IGraphicsBuffer buffer = loadedMaps[sector];
                buffer.Draw(renderTarget, minX, maxX, minY, maxY, cameraZoom);
            }
            if (previewSquare != null)
            {
                var basicEffect = new BasicEffect(graphicsDevice);
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
                basicEffect.TextureEnabled = false;
                basicEffect.VertexColorEnabled = true;
                basicEffect.LightingEnabled = false;
                float minLat = (float)ToLat(ToY(previewSquare.Latitude) - previewSquare.ZoomPortion / 2);
                float maxLat = (float)ToLat(ToY(previewSquare.Latitude) + previewSquare.ZoomPortion / 2);
                float minLong = (float)(previewSquare.Longitude - Math.PI * previewSquare.ZoomPortion);
                float maxLong = (float)(previewSquare.Longitude + Math.PI * previewSquare.ZoomPortion);
                float w = maxLong - minLong;
                float h = maxLat - minLat;
                Color color = CacheExists(previewSquare) ? Color.Green : Color.Red;
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w / 20, h, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w, h / 20, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong + w * 19 / 20, minLat, w / 20, h, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat + h * 19 / 20, w, h / 20, color);
            }
        }

        public abstract bool CacheExists(MercatorSector sector);
        public abstract bool DoAutoLoad(MercatorSector sector); // safe to autoload? (fast/already cached)
        public abstract bool AllowUnload(MercatorSector sector); // allowed to unload? (cached)

        public void Update(double mouseX, double mouseY, double cameraZoom)
        {
            if (UILayer.LeftPressed) AddImage(mouseX, mouseY, cameraZoom);
            if (UILayer.LeftAvailable)
            {
                previewSquare = GetSector(mouseX, mouseY, cameraZoom);
            }
            else
            {
                previewSquare = null;
            }
        }

        private int GetRoundedZoom(double cameraZoom)
        {
            return Math.Min((int)cameraZoom, MAX_ZOOM); // google only accepts integer zoom
        }

        private MercatorSector GetSector(double mouseX, double mouseY, double cameraZoom)
        {
            int zoom = GetRoundedZoom(cameraZoom);
            double zoomPortion = Math.Pow(0.5, zoom);
            int x = (int)((mouseX + Math.PI) / (zoomPortion * 2 * Math.PI));
            int y = (int)(ToY(mouseY) / (zoomPortion));
            y = Math.Max(y, 0);
            y = Math.Min(y, (1 << zoom) - 1);
            return new MercatorSector(x, y, zoom);
        }

        public abstract IEnumerable<MercatorSector> EnumerateCachedSectors();

        private void LoadAllCached(GraphicsDevice graphicsDevice)
        {
            //foreach (var sector in EnumerateCachedSectors())
            //{
            //    imageLayers[sector.zoom].Add(sector);
            //}
        }

        private void AddImage(double mouseX, double mouseY, double cameraZoom)
        {
            MercatorSector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            toLoad.Add(squareCenter);
        }

        public abstract IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, ISector sector);

        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }

        // takes -pi/2 to pi/2, I assume, goes from -infinity to infinity??
        // goes from 0 to 1 in the cutoff range of 85.051129 degrees
        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }

        public List<string> GetDebugInfo()
        {
            return new List<string>();
        }

        public List<IUIComponent> GetSettings()
        {
            return new List<IUIComponent>();
        }

        public List<IEditorGameComponent> GetSubComponents()
        {
            return new List<IEditorGameComponent>();
        }
    }
}

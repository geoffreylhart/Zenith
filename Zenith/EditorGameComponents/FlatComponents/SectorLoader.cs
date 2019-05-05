using System;
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
        Dictionary<Sector, IGraphicsBuffer> loadedMaps = new Dictionary<Sector, IGraphicsBuffer>();
        HashSet<Sector> toLoad = new HashSet<Sector>();
        Sector previewSquare = null;

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            // autoload stuff
            // TODO: move to update step?
            int zoomLevel = (int)(Math.Log((maxX - minX) / (2 * Math.PI)) / Math.Log(0.5));
            Sector bottomLeft = GetSector(minX, minY, zoomLevel);
            Sector bottomRight = GetSector(maxX, minY, zoomLevel);
            Sector topLeft = GetSector(minX, maxY, zoomLevel);
            Sector topRight = GetSector(maxX, maxY, zoomLevel);
            List<Sector> containedSectors = new List<Sector>();
            for (int i = bottomLeft.x; i <= bottomRight.x; i++)
            {
                for (int j = bottomLeft.y; j <= topLeft.y; j++)
                {
                    containedSectors.Add(new Sector(i % (1 << bottomLeft.zoom), j % (1 << bottomLeft.zoom), bottomLeft.zoom));
                }
            }
            List<Sector> unload = new List<Sector>();
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
            toLoad = new HashSet<Sector>();
            List<Sector> sorted = loadedMaps.Keys.ToList();
            sorted.Sort((x, y) => x.zoom.CompareTo(y.zoom));
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

        public abstract bool CacheExists(Sector sector);
        public abstract bool DoAutoLoad(Sector sector); // safe to autoload? (fast/already cached)
        public abstract bool AllowUnload(Sector sector); // allowed to unload? (cached)

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

        private Sector GetSector(double mouseX, double mouseY, double cameraZoom)
        {
            int zoom = GetRoundedZoom(cameraZoom);
            double zoomPortion = Math.Pow(0.5, zoom);
            int x = (int)((mouseX + Math.PI) / (zoomPortion * 2 * Math.PI));
            int y = (int)(ToY(mouseY) / (zoomPortion));
            y = Math.Max(y, 0);
            y = Math.Min(y, (1 << zoom) - 1);
            return new Sector(x, y, zoom);
        }

        public abstract IEnumerable<Sector> EnumerateCachedSectors();

        private void LoadAllCached(GraphicsDevice graphicsDevice)
        {
            //foreach (var sector in EnumerateCachedSectors())
            //{
            //    imageLayers[sector.zoom].Add(sector);
            //}
        }

        private void AddImage(double mouseX, double mouseY, double cameraZoom)
        {
            Sector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            toLoad.Add(squareCenter);
        }

        public abstract IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, Sector sector);

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

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
using Zenith.ZGeom;
using Zenith.ZGraphics;
using Zenith.ZGraphics.GraphicsBuffers;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    abstract class SectorLoader : IFlatComponent, IEditorGameComponent
    {
        private static int MAX_ZOOM = 19;
        Dictionary<ISector, IGraphicsBuffer> loadedMaps = new Dictionary<ISector, IGraphicsBuffer>();
        HashSet<ISector> toLoad = new HashSet<ISector>();
        ISector previewSquare = null;

        public void Draw(RenderTarget2D renderTarget, ISector rootSector, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            // autoload stuff
            // TODO: move to update step?
            int zoomLevel = (int)(Math.Log(maxX - minX) / Math.Log(0.5));
            List<ISector> containedSectors = rootSector.GetSectorsInRange(minX, maxX, minY, maxY, zoomLevel);
            List<ISector> unload = new List<ISector>();
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
            toLoad = new HashSet<ISector>();
            List<ISector> sorted = loadedMaps.Keys.ToList();
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
                float minLat = (float)(previewSquare.ZoomPortion * previewSquare.Y);
                float maxLat = (float)(previewSquare.ZoomPortion * (previewSquare.Y + 1));
                float minLong = (float)(previewSquare.ZoomPortion * previewSquare.X);
                float maxLong = (float)(previewSquare.ZoomPortion * (previewSquare.X + 1));
                float w = maxLong - minLong;
                float h = maxLat - minLat;
                Color color = CacheExists(previewSquare) ? Color.Green : Color.Red;
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w / 20, h, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w, h / 20, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong + w * 19 / 20, minLat, w / 20, h, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat + h * 19 / 20, w, h / 20, color);
            }
        }

        public abstract bool CacheExists(ISector sector);
        public abstract bool DoAutoLoad(ISector sector); // safe to autoload? (fast/already cached)
        public abstract bool AllowUnload(ISector sector); // allowed to unload? (cached)

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

        private ISector GetSector(double mouseX, double mouseY, double cameraZoom)
        {
            int zoom = GetRoundedZoom(cameraZoom);
            LongLat longLat = new LongLat(mouseX, mouseY);
            foreach (var sector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                if (sector.ContainsLongLat(longLat))
                {
                    var localCoord = sector.ProjectToLocalCoordinates(longLat.ToSphereVector());
                    var sectorAt = sector.GetSectorAt(localCoord.X, localCoord.Y, zoom);
                    if (sectorAt != null) return sectorAt;
                }
            }
            return null;
        }

        public abstract IEnumerable<ISector> EnumerateCachedSectors();

        private void LoadAllCached(GraphicsDevice graphicsDevice)
        {
            //foreach (var sector in EnumerateCachedSectors())
            //{
            //    imageLayers[sector.zoom].Add(sector);
            //}
        }

        private void AddImage(double mouseX, double mouseY, double cameraZoom)
        {
            ISector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            toLoad.Add(squareCenter);
        }

        public abstract IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, ISector sector);

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

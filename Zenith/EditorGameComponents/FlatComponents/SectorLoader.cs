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
        ISector toLoad = null;
        ISector previewSquare = null;

        public void Draw(RenderTarget2D renderTarget, ISector rootSector, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            // autoload stuff
            // TODO: move to update step?
            int zoomLevel;
            if (rootSector is MercatorSector)
            {
                zoomLevel = Math.Max((int)(Math.Log(maxX - minX) / Math.Log(0.5)), 0);
            }
            else
            {
                zoomLevel = Math.Max((int)(Math.Log(maxX - minX) / Math.Log(0.5) - 3), 0);
            }
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
            // end autoload stuff
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            if (toLoad != null)
            {
                if (loadedMaps.ContainsKey(toLoad)) loadedMaps[toLoad].Dispose();
                loadedMaps[toLoad] = GetGraphicsBuffer(renderTarget.GraphicsDevice, toLoad);
            }
            foreach (var l in containedSectors)
            {
                if (!loadedMaps.ContainsKey(l))
                {
                    loadedMaps[l] = GetCacheBuffer(renderTarget.GraphicsDevice, l);
                }
            }
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
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w / 20, h, Color.Red);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w, h / 20, Color.Red);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong + w * 19 / 20, minLat, w / 20, h, Color.Red);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat + h * 19 / 20, w, h / 20, Color.Red);
            }
        }

        private bool AllowUnload(ISector sector)
        {

            return sector.Zoom <= ZCoords.GetSectorManager().GetHighestOSMZoom() - 3;
        }

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

        private void AddImage(double mouseX, double mouseY, double cameraZoom)
        {
            ISector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            toLoad = squareCenter;
        }

        public abstract IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, ISector sector);

        public abstract IGraphicsBuffer GetCacheBuffer(GraphicsDevice graphicsDevice, ISector sector);

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

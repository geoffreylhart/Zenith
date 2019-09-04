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
            double relativeCameraZoom = cameraZoom - Math.Log(ZCoords.GetSectorManager().GetTopmostOSMSectors().Count, 4);
            // autoload stuff
            // TODO: move to update step?
            int zoomLevel = Math.Min(Math.Max((int)(relativeCameraZoom - 3), 0), ZCoords.GetSectorManager().GetHighestOSMZoom());
            List<ISector> containedSectors = rootSector.GetSectorsInRange(minX, maxX, minY, maxY, zoomLevel);
            foreach (var pair in loadedMaps.Where(x => AllowUnload(x.Key, rootSector, containedSectors)).ToList())
            {
                loadedMaps[pair.Key].Dispose();
                loadedMaps.Remove(pair.Key);
            }
            // end autoload stuff
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            if (toLoad != null)
            {
                if (loadedMaps.ContainsKey(toLoad)) loadedMaps[toLoad].Dispose();
                loadedMaps[toLoad] = GetGraphicsBuffer(renderTarget.GraphicsDevice, toLoad);
                toLoad = null;
            }
            bool loadCache = !(relativeCameraZoom - 4 > ZCoords.GetSectorManager().GetHighestOSMZoom());
            foreach (var l in containedSectors)
            {
                if (loadCache)
                {
                    if (!loadedMaps.ContainsKey(l))
                    {
                        loadedMaps[l] = GetCacheBuffer(renderTarget.GraphicsDevice, l);
                    }
                }
                else
                {
                    if (!loadedMaps.ContainsKey(l) || loadedMaps[l] is ImageTileBuffer)
                    {
                        if (loadedMaps.ContainsKey(l)) loadedMaps[l].Dispose();
                        loadedMaps[l] = GetGraphicsBuffer(renderTarget.GraphicsDevice, l);
                    }
                }
            }
            List<ISector> sorted = containedSectors.Where(x => x.GetRoot().Equals(rootSector)).ToList();
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

        private bool AllowUnload(ISector sector, ISector rootSector, List<ISector> loadingSectors)
        {
            if (loadedMaps[sector] is ProceduralTileBuffer) return false; // TODO: eventually unload these, maybe just have a queue
            if (!sector.GetRoot().Equals(rootSector)) return false;
            foreach (var s in loadingSectors)
            {
                if (s.Equals(sector)) return false; // very basic: don't unload sectors immediately after loading them
            }
            return true;
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

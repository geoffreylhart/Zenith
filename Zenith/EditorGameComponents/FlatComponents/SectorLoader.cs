using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.PrimitiveBuilder;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents.FlatComponents
{
    abstract class SectorLoader : IFlatComponent, IEditorGameComponent
    {
        private static int MAX_ZOOM = 19;
        Dictionary<String, VertexIndiceBuffer> loadedMaps = new Dictionary<string, VertexIndiceBuffer>();
        List<Sector>[] imageLayers = new List<Sector>[MAX_ZOOM + 1];
        Sector previewSquare = null;

        public SectorLoader()
        {
            for (int i = 0; i <= MAX_ZOOM; i++) imageLayers[i] = new List<Sector>();
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);

            foreach (var layer in imageLayers)
            {
                foreach (var sector in layer)
                {
                    if (!loadedMaps.ContainsKey(sector.ToString()))
                    {
                        loadedMaps[sector.ToString()] = GetCachedMap(renderTarget, sector);
                    }
                    VertexIndiceBuffer buffer = loadedMaps[sector.ToString()];
                    basicEffect.Texture = buffer.texture;
                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.Indices = buffer.indices;
                        graphicsDevice.SetVertexBuffer(buffer.vertices);
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                    }
                    graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, graphicsDevice.Viewport.MaxDepth, 0);
                }
            }
            if (previewSquare != null)
            {
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
            foreach (var sector in EnumerateCachedSectors())
            {
                imageLayers[sector.zoom].Add(sector);
            }
        }

        private void AddImage(double mouseX, double mouseY, double cameraZoom)
        {
            Sector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            imageLayers[squareCenter.zoom].Add(squareCenter);
        }

        public abstract Texture2D GetTexture(GraphicsDevice graphicsDevice, Sector sector);

        private VertexIndiceBuffer GetCachedMap(RenderTarget2D renderTarget, Sector sector)
        {
            VertexIndiceBuffer buffer = SphereBuilder.MapMercatorToCylindrical(renderTarget.GraphicsDevice, 2, Math.Pow(0.5, sector.zoom), sector.Latitude, sector.Longitude);
            buffer.texture = GetTexture(renderTarget.GraphicsDevice, sector);
            renderTarget.GraphicsDevice.SetRenderTarget(renderTarget); // TODO: let's just assume this won't cause issues for now
            return buffer;
        }

        public class Sector
        {
            public int x; // measured 0,1,2,3 from -pi to pi (opposite left to opposite right of prime meridian)
            public int y; // measured 0,1,2,3 from -pi/2 (south pole) to pi/2 (north pole)
            public int zoom; // the globe is partitioned into 2^zoom vertical and horizontal sections

            public Sector(int x, int y, int zoom)
            {
                this.x = x;
                this.y = y;
                this.zoom = zoom;
            }

            public double ZoomPortion { get { return Math.Pow(0.5, zoom); } }
            public double Longitude { get { return (x + 0.5) * (ZoomPortion * 2 * Math.PI) - Math.PI; } }
            public double Latitude { get { return ToLat((y + 0.5) * (ZoomPortion)); } }

            public override string ToString()
            {
                return $"X={x},Y={y},Zoom={zoom}";
            }

            public bool ContainsSector(Sector sector)
            {
                if (sector.zoom < zoom) return false;
                if ((sector.x >> (sector.zoom - zoom)) != x) return false;
                if ((sector.y >> (sector.zoom - zoom)) != y) return false;
                return true;
            }
        }



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

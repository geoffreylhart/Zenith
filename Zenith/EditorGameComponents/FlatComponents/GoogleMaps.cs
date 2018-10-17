using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class GoogleMaps : IFlatComponent
    {
        private static int MAX_ZOOM = 19;
        Dictionary<String, VertexIndiceBuffer> loadedMaps = new Dictionary<string, VertexIndiceBuffer>();
        List<Sector>[] googleMapLayers = new List<Sector>[MAX_ZOOM + 1];
        Sector previewSquare = null;

        public GoogleMaps()
        {
            for (int i = 0; i <= MAX_ZOOM; i++) googleMapLayers[i] = new List<Sector>();
        }

        public void Draw(GraphicsDevice graphicsDevice, double minX, double maxX, double minY, double maxY)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);

            foreach (var layer in googleMapLayers)
            {
                foreach (var sector in layer)
                {
                    if (!loadedMaps.ContainsKey(sector.ToString()))
                    {
                        loadedMaps[sector.ToString()] = GetCachedMap(graphicsDevice, sector);
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

        private bool CacheExists(Sector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            String filePath = @"..\..\..\..\LocalCache\" + fileName;
            return File.Exists(filePath);
        }

        public void Update(double mouseX, double mouseY, double cameraZoom)
        {
            if (UILayer.LeftPressed) AddGoogleMap(mouseX, mouseY, cameraZoom);
            previewSquare = GetSector(mouseX, mouseY, cameraZoom);
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

        private void LoadAll(GraphicsDevice graphicsDevice)
        {
            foreach (var file in Directory.EnumerateFiles(@"..\..\..\..\LocalCache"))
            {
                String filename = Path.GetFileName(file);
                if (filename.StartsWith("X"))
                {
                    String[] split = filename.Split(',');
                    int x = int.Parse(split[0].Split('=')[1]);
                    int y = int.Parse(split[1].Split('=')[1]);
                    int zoom = int.Parse(split[2].Split('=', '.')[1]);
                    Sector newSec = new Sector(x, y, zoom);
                    googleMapLayers[zoom].Add(newSec);
                }
            }
        }

        private void AddGoogleMap(double mouseX, double mouseY, double cameraZoom)
        {
            Sector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            googleMapLayers[squareCenter.zoom].Add(squareCenter);
        }

        private VertexIndiceBuffer GetCachedMap(GraphicsDevice graphicsDevice, Sector sector)
        {
            VertexIndiceBuffer buffer = SphereBuilder.MapMercatorToCylindrical(graphicsDevice, 2, Math.Pow(0.5, sector.zoom), sector.Latitude, sector.Longitude);
            String fileName = sector.ToString() + ".PNG";
            String filePath = @"..\..\..\..\LocalCache\" + fileName;
            if (File.Exists(filePath))
            {
                using (var reader = File.OpenRead(filePath))
                {
                    buffer.texture = Texture2D.FromStream(graphicsDevice, reader);
                }
            }
            else
            {
                buffer.texture = MapGenerator.GetMap(graphicsDevice, sector.Longitude * 180 / Math.PI, sector.Latitude * 180 / Math.PI, sector.zoom);
                using (var writer = File.OpenWrite(filePath))
                {
                    buffer.texture.SaveAsPng(writer, buffer.texture.Width, buffer.texture.Height);
                }
            }
            return buffer;
        }

        private class Sector
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
        }



        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }

        // takes -pi/2 to pi/2, I assume, goes from -infinity to infinity??
        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }
    }
}

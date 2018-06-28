using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;

namespace Zenith.EditorGameComponents
{
    public class GoogleMaps : DrawableGameComponent
    {
        private EditorCamera camera;
        List<VertexIndiceBuffer> googleMaps = new List<VertexIndiceBuffer>();
        VertexBuffer previewSquare = null;

        public GoogleMaps(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Draw(GameTime gameTime)
        {
            var basicEffect3 = MakeThatBasicEffect3();
            basicEffect3.TextureEnabled = true;
            foreach (var buffer in googleMaps)
            {
                basicEffect3.Texture = buffer.texture;
                foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.Indices = buffer.indices;
                    GraphicsDevice.SetVertexBuffer(buffer.vertices);
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                }
                GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, GraphicsDevice.Viewport.MaxDepth, 0);
            }
            if (previewSquare != null && Enabled)
            {
                basicEffect3.TextureEnabled = false;
                basicEffect3.VertexColorEnabled = true;
                basicEffect3.LightingEnabled = false;
                foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.SetVertexBuffer(previewSquare);
                    GraphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, previewSquare.VertexCount - 1);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (Mouse.GetState().WasLeftPressed()) AddGoogleMap();
            if (previewSquare != null) previewSquare.Dispose();
            previewSquare = null;
            Sector squareCenter = GetSector();
            if (squareCenter != null)
            {
                int googleZoom = GetRoundedZoom();
                double zoomPortion = Math.Pow(0.5, googleZoom);
                previewSquare = SphereBuilder.MakeSphereSegOutlineLatLong(GraphicsDevice, 2, zoomPortion, squareCenter.Latitude, squareCenter.Longitude);
            }
        }

        private int GetRoundedZoom()
        {
            return Math.Min((int)camera.cameraZoom, 19); // google only accepts integer zoom
        }

        private void AddGoogleMap()
        {
            Sector squareCenter = GetSector();
            if (squareCenter == null) return;
            googleMaps.Add(GetCachedMap(squareCenter));
        }

        private VertexIndiceBuffer GetCachedMap(Sector sector)
        {
            VertexIndiceBuffer buffer = SphereBuilder.MakeSphereSegLatLong(GraphicsDevice, 2, Math.Pow(0.5, sector.zoom), sector.Latitude, sector.Longitude);
            String fileName = sector.ToString() + ".PNG";
            String filePath = @"..\..\..\..\LocalCache\" + fileName;
            if (File.Exists(filePath))
            {
                using (var reader = File.OpenRead(filePath))
                {
                    buffer.texture = Texture2D.FromStream(GraphicsDevice, reader);
                }
            }
            else
            {
                buffer.texture = MapGenerator.GetMap(GraphicsDevice, sector.Longitude * 180 / Math.PI, sector.Latitude * 180 / Math.PI, sector.zoom);
                using (var writer = File.OpenWrite(filePath))
                {
                    buffer.texture.SaveAsPng(writer, buffer.texture.Width, buffer.texture.Height);
                }
            }
            return buffer;
        }

        private BasicEffect MakeThatBasicEffect3()
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            // basicEffect3.LightingEnabled = true;
            // basicEffect3.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            // basicEffect3.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            // basicEffect3.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            camera.ApplyMatrices(basicEffect3);
            return basicEffect3;
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

        /// <summary>
        /// Returns the sector the mouse is currently hovering over
        /// </summary>
        /// <returns>The sector the mouse is currently hovering over</returns>
        private Sector GetSector()
        {
            int zoom = GetRoundedZoom();
            double zoomPortion = Math.Pow(0.5, zoom);
            Vector2 mouseVector = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Vector3d squareCenter = camera.GetLatLongOfCoord2(Mouse.GetState().X, Mouse.GetState().Y);
            if (squareCenter == null) return null;
            int x = (int)((squareCenter.X + Math.PI) / (zoomPortion * 2 * Math.PI));
            int y = (int)((ToY(squareCenter.Y)) / (zoomPortion));
            y = Math.Max(y, 0);
            y = Math.Min(y, (1 << zoom) - 1);
            return new Sector(x, y, zoom);
        }

        private class Sector
        {
            public int x;
            public int y;
            public int zoom;

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
    }
}

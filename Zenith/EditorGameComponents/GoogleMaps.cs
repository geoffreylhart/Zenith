using System;
using System.Collections.Generic;
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
            Vector3d squareCenter = GetSnappedCoordinate();
            if (squareCenter != null)
            {
                int googleZoom = (int)camera.cameraZoom;
                double zoomPortion = Math.Pow(0.5, googleZoom);
                previewSquare = SphereBuilder.MakeSphereSegOutlineLatLong(GraphicsDevice, 2, zoomPortion, squareCenter.Y, squareCenter.X);
            }
        }

        private Vector3d GetSnappedCoordinate()
        {
            int googleZoom = (int)camera.cameraZoom;
            double zoomPortion = Math.Pow(0.5, googleZoom);
            Vector2 mouseVector = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Vector3d squareCenter = camera.GetLatLongOfCoord2(Mouse.GetState().X, Mouse.GetState().Y);
            if (squareCenter == null) return null;
            int intX = (int)((squareCenter.X + Math.PI) / (zoomPortion * 2 * Math.PI));
            int intY = (int)((squareCenter.Y + Math.PI / 2) / (zoomPortion * 2 * Math.PI));
            squareCenter.X = (intX + 0.5) * (zoomPortion * 2 * Math.PI) - Math.PI;
            squareCenter.Y = (intY + 0.5) * (zoomPortion * 2 * Math.PI) - Math.PI / 2;
            return squareCenter;
        }

        private void AddGoogleMap()
        {
            Vector2 mouseVector = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Vector3d squareCenter = GetSnappedCoordinate();
            if (squareCenter == null) return;
            int googleZoom = (int)camera.cameraZoom; // I guess Google only accepts integer zoom?
            VertexIndiceBuffer buffer = SphereBuilder.MakeSphereSegLatLong(GraphicsDevice, 2, Math.Pow(0.5, googleZoom), squareCenter.Y, squareCenter.X);
            buffer.texture = MapGenerator.GetMap(GraphicsDevice, squareCenter.X * 180 / Math.PI, squareCenter.Y * 180 / Math.PI, googleZoom);
            googleMaps.Add(buffer);
        }

        private BasicEffect MakeThatBasicEffect3()
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.LightingEnabled = true;
            basicEffect3.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            basicEffect3.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect3.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            camera.ApplyMatrices(basicEffect3);
            return basicEffect3;
        }
    }
}

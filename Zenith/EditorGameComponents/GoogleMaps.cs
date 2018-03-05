using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;
using Zenith.PrimitiveBuilder;

namespace Zenith.EditorGameComponents
{
    public class GoogleMaps : DrawableGameComponent
    {
        private EditorCamera camera;
        List<VertexIndiceBuffer> googleMaps = new List<VertexIndiceBuffer>();

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
        }

        public override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().WasKeyPressed(Keys.G)) AddGoogleMap();
        }

        private void AddGoogleMap()
        {
            int googleZoom = (int)camera.cameraZoom; // I guess Google only accepts integer zoom?
            VertexIndiceBuffer buffer = SphereBuilder.MakeSphereSegLatLong(GraphicsDevice, 2, Math.Pow(0.5, googleZoom), camera.cameraRotY, camera.cameraRotX);
            buffer.texture = MapGenerator.GetMap(GraphicsDevice, camera.cameraRotY * 180 / Math.PI, camera.cameraRotX * 180 / Math.PI, googleZoom);
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

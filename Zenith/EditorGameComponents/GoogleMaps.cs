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
            //sphere = SphereBuilder.MakeSphereSeg(GraphicsDevice, 2, 1, 0, 0);
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
                GraphicsDevice.Clear(
                    ClearOptions.DepthBuffer,
                    Microsoft.Xna.Framework.Color.Transparent,
                    GraphicsDevice.Viewport.MaxDepth,
                    0);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().WasKeyPressed(Keys.G)) AddGoogleMap();
        }

        private void AddGoogleMap()
        {
            int googleZoom = (int)camera.cameraZoom; // I guess Google only accepts integer zoom?
            // TODO: why are all of the images upside down and why did I have to flip the latitude?
            VertexIndiceBuffer buffer = SphereBuilder.MakeSphereSegLatLong(GraphicsDevice, 2, Math.Pow(0.5, googleZoom), camera.cameraRotY, -camera.cameraRotX + Math.PI / 2);
            buffer.texture = MapGenerator.GetMap(GraphicsDevice, camera.cameraRotY * 180 / Math.PI, (-camera.cameraRotX + Math.PI / 2) * 180 / Math.PI, googleZoom);
            googleMaps.Add(buffer);
        }

        private BasicEffect MakeThatBasicEffect3()
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.LightingEnabled = true;
            basicEffect3.DirectionalLight0.Direction = new Vector3(1, -1, 0);
            basicEffect3.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect3.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            //basicEffect3.World = Matrix.CreateRotationX((float)cameraRotY) * Matrix.CreateRotationZ((float)cameraRotX);
            basicEffect3.World = Matrix.CreateRotationZ((float)camera.cameraRotX) * Matrix.CreateRotationX((float)camera.cameraRotY);
            float distance = (float)(Math.Pow(0.5, camera.cameraZoom) * 19);
            // TODO: cheated here and just turned the camera upside down
            basicEffect3.View = Matrix.CreateLookAt(new Vector3(0, -20, 0), new Vector3(0, 0, 0), -Vector3.UnitZ); // we'll match Blender for "up" of camera and camera starting position
            // TODO: I just have no clue why the camera isn't working like it used to where I just move the camera closer and closer
            basicEffect3.Projection = Matrix.CreatePerspectiveFieldOfView((float)((Math.PI / 4) * Math.Pow(0.5, camera.cameraZoom)), 800f / 480f, 0.1f, 100); // was 0.1f and 100f
            //Debug(-distance - 1f);
            return basicEffect3;
        }
    }
}

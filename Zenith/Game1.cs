using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Zenith.PrimitiveBuilder;

namespace Zenith
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        VertexIndiceBuffer box;
        double cameraRotX = 0;
        double cameraRotY = 0;
        static double CAMERA_ROT_VEL = 0.01;


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            box = CubeBuilder.MakeBasicCube(GraphicsDevice);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                cameraRotX -= CAMERA_ROT_VEL;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                cameraRotX += CAMERA_ROT_VEL;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                cameraRotY -= CAMERA_ROT_VEL;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                cameraRotY += CAMERA_ROT_VEL;
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.LightingEnabled = true;
            basicEffect3.DirectionalLight0.Direction = new Vector3(-1, 0, 0);
            basicEffect3.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect3.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            //basicEffect3.AmbientLightColor = new Vector3(1, 1, 1);
            basicEffect3.World = Matrix.CreateRotationX((float)cameraRotY) * Matrix.CreateRotationZ((float)cameraRotX);
            //basicEffect3.World = Matrix.Identity;
            basicEffect3.View = Matrix.CreateLookAt(new Vector3(0, -20, 0), new Vector3(0, 0, 0), Vector3.UnitZ); // we'll match Blender for "up" of camera and camera starting position
            basicEffect3.Projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), 800f / 480f, 0.1f, 100f);
            foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.Indices = box.indices;
                GraphicsDevice.SetVertexBuffer(box.vertices);
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, box.indices.IndexCount / 3);
            }
            base.Draw(gameTime);
        }
    }
}

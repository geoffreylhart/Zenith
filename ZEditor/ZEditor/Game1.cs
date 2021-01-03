using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using ZEditor.ZGraphics;

namespace ZEditor
{
    public class Game1 : Game
    {
        private VertexIndexBuffer renderSubject;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            renderSubject = GenerateCube(GraphicsDevice);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            BasicEffect basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.World = Matrix.Identity;
            // note, after WPV is applied (or is it WVP?), I believe near plane matches to 1 and far plane matches to -1, matching the handedness of Vector3 Constants
            basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), 1.5f, 0.01f, 10f);
            // the camera position and lookup at least match up to the coordinates/colors we gave
            basicEffect.View = Matrix.CreateLookAt(new Vector3(2, 2, -1), new Vector3(0.5f, 0.5f, 0.5f), Vector3.Up);
            basicEffect.VertexColorEnabled = true;
            GraphicsDevice.SetVertexBuffer(renderSubject.vertexBuffer);
            GraphicsDevice.Indices = renderSubject.indexBuffer;
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }

            base.Draw(gameTime);
        }

        private static VertexIndexBuffer GenerateCube(GraphicsDevice graphicsDevice)
        {
            var cullMode = graphicsDevice.RasterizerState.CullMode; // is set to cull counterclockwiseface
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            List<int> indices = new List<int>();
            // according to vector3, 1,1,1 is right, up, backward
            // atm we will construct the cube assuming this is from a spaceship perspective
            // this means that the 111, would be the lefttopback from outside perspective
            vertices.Add(new VertexPositionColor(new Vector3(0, 0, 0), Color.White));
            vertices.Add(new VertexPositionColor(new Vector3(0, 0, 1), Color.Red));
            vertices.Add(new VertexPositionColor(new Vector3(0, 1, 0), Color.Blue));
            vertices.Add(new VertexPositionColor(new Vector3(0, 1, 1), Color.Green));
            vertices.Add(new VertexPositionColor(new Vector3(1, 0, 0), Color.Black));
            vertices.Add(new VertexPositionColor(new Vector3(1, 0, 1), Color.Cyan));
            vertices.Add(new VertexPositionColor(new Vector3(1, 1, 0), Color.Yellow));
            vertices.Add(new VertexPositionColor(new Vector3(1, 1, 1), Color.Magenta));
            // top, bottom, left (from outside perspective), right, front, back
            AddIndices(indices, 7, 3, 2, 6); // lefttopback, righttopback, righttopfront, lefttopfront
            AddIndices(indices, 4, 0, 1, 5); // leftbottomfront, rightbottomfront, rightbottomback, leftbottomback
            AddIndices(indices, 7, 6, 4, 5); // lefttopback, lefttopfront, leftbottomfront, leftbottomback
            AddIndices(indices, 2, 3, 1, 0); // righttopfront, righttopback, rightbottomback, rightbottomfront
            AddIndices(indices, 6, 2, 0, 4); // lefttopfront, righttopfront, rightbottomfront, leftbottomfront
            AddIndices(indices, 3, 7, 5, 1); // righttopback, lefttopback, leftbottomback, rightbottomback

            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());
            return new VertexIndexBuffer(vertexBuffer, indexBuffer);
        }

        private static void AddIndices(List<int> indices, int topLeft, int topRight, int bottomRight, int bottomLeft)
        {
            // preferred quad order topleft, topright, bottomright, topleft, bottomright, bottomleft
            indices.Add(topLeft);
            indices.Add(topRight);
            indices.Add(bottomRight);
            indices.Add(topLeft);
            indices.Add(bottomRight);
            indices.Add(bottomLeft);
        }
    }
}

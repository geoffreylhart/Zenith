using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using ZEditor.ZControl;
using ZEditor.ZGraphics;
using ZEditor.ZManage;
using ZEditor.ZTemplates;

namespace ZEditor
{
    public class Game1 : Game
    {
        private VertexIndexBuffer renderSubjectFaceBuffer;
        private VertexIndexBuffer renderSubjectLineBuffer;
        private VertexIndexBuffer renderSubjectPointBuffer;
        private Effect pointsShader;
        private ITemplate renderSubject;
        private AbstractCamera camera;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D cursorTexture;
        private bool editMode = false;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            //IsMouseVisible = false;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1 / 144.0);
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            renderSubject = TemplateManager.Load("zdata.txt", "Spaceship1");
            renderSubjectFaceBuffer = renderSubject.MakeFaceBuffer(GraphicsDevice);
            renderSubjectLineBuffer = renderSubject.MakeLineBuffer(GraphicsDevice);
            renderSubjectPointBuffer = renderSubject.MakePointBuffer(GraphicsDevice);
            // view from slightly above and to the right, but far away TODO: for some reason we aren't looking at 0, 0, 0??
            camera = new FPSCamera(new Vector3(-2, 2, -10), new Vector3(0, 0, 0));
            int radii = 5;
            cursorTexture = new Texture2D(GraphicsDevice, radii * 2 + 1, radii * 2 + 1);
            Color[] data = new Color[(radii * 2 + 1) * (radii * 2 + 1)];
            for (int i = 0; i < radii * 2 + 1; i++)
            {
                data[i + (radii * 2 + 1) * radii] = Color.White; // horizontal
                data[i * (radii * 2 + 1) + radii] = Color.White; // vertical
            }
            cursorTexture.SetData(data);

            pointsShader = Content.Load<Effect>("Shaders/PointsShader");

            int w = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int h = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferWidth = w;
            _graphics.PreferredBackBufferHeight = h;
            //_graphics.IsFullScreen = true;
            _graphics.ApplyChanges();

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

            if (Keyboard.GetState().AreKeysCtrlPressed(Keys.E))
            {
                editMode = !editMode;
                if (editMode)
                {
                    camera = new EditorCamera(camera.GetPosition(), camera.GetTarget());
                }
                else
                {
                    camera = new FPSCamera(camera.GetPosition(), camera.GetTarget());
                }
            }

            // TODO: Add your update logic here
            camera.Update(gameTime, Keyboard.GetState(), Mouse.GetState(), GraphicsDevice);
            renderSubject.Update(gameTime, Keyboard.GetState(), Mouse.GetState(), camera, GraphicsDevice, editMode);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            BasicEffect basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.World = Matrix.Identity;
            // note, after WVP is applied, I believe near plane matches to 1 and far plane matches to -1, matching the handedness of Vector3 Constants
            // the camera position and lookup at least match up to the coordinates/colors we gave
            basicEffect.View = camera.GetView();
            basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), GraphicsDevice.Viewport.AspectRatio, 0.01f, 100f);
            basicEffect.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.Enabled = true;
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.9f, 0.9f, 0.9f);
            var direction = new Vector3(2, -2, 10);
            direction.Normalize();
            basicEffect.DirectionalLight0.Direction = direction;
            // points effect
            Effect pointsEffect = pointsShader;
            pointsShader.Parameters["WVP"].SetValue(basicEffect.World * basicEffect.View * basicEffect.Projection);
            pointsShader.Parameters["PointSize"].SetValue(new Vector2(10f / GraphicsDevice.Viewport.Width, 10f / GraphicsDevice.Viewport.Height));
            if (editMode)
            {
                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.9f, 0.9f, 0.9f);
                var direction2 = new Vector3(-2, 2, -10);
                direction2.Normalize();
                basicEffect.DirectionalLight1.Direction = direction2;
                Render(GraphicsDevice, basicEffect, renderSubjectFaceBuffer, PrimitiveType.TriangleList, renderSubjectFaceBuffer.indexBuffer.IndexCount / 3);
                Render(GraphicsDevice, basicEffect, renderSubjectLineBuffer, PrimitiveType.LineList, renderSubjectLineBuffer.indexBuffer.IndexCount / 2);
                Render(GraphicsDevice, pointsShader, renderSubjectPointBuffer, PrimitiveType.TriangleList, renderSubjectPointBuffer.indexBuffer.IndexCount / 3);
            }
            else
            {
                Render(GraphicsDevice, basicEffect, renderSubjectFaceBuffer, PrimitiveType.TriangleList, renderSubjectFaceBuffer.indexBuffer.IndexCount / 3);
            }
            // draw cursor
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            _spriteBatch.Draw(cursorTexture, new Vector2(Mouse.GetState().X, Mouse.GetState().Y), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void Render(GraphicsDevice graphicsDevice, Effect effect, VertexIndexBuffer buffer, PrimitiveType primitiveType, int primitiveCount)
        {
            graphicsDevice.SetVertexBuffer(buffer.vertexBuffer);
            graphicsDevice.Indices = buffer.indexBuffer;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, primitiveCount);
            }
        }
    }
}

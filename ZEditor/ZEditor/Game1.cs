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
        private ITemplate renderSubject;
        private FPSCamera fpsCamera;
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
            // view from slightly above and to the right, but far away TODO: for some reason we aren't looking at 0, 0, 0??
            fpsCamera = new FPSCamera(new Vector3(-2, 2, -10), new Vector3(0, 0, 0));
            int radii = 5;
            cursorTexture = new Texture2D(GraphicsDevice, radii * 2 + 1, radii * 2 + 1);
            Color[] data = new Color[(radii * 2 + 1) * (radii * 2 + 1)];
            for (int i = 0; i < radii * 2 + 1; i++)
            {
                data[i + (radii * 2 + 1) * radii] = Color.White; // horizontal
                data[i * (radii * 2 + 1) + radii] = Color.White; // vertical
            }
            cursorTexture.SetData(data);

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

            editMode ^= Keyboard.GetState().AreKeysCtrlPressed(Keys.E);

            // TODO: Add your update logic here
            fpsCamera.Update(gameTime, Keyboard.GetState(), Mouse.GetState(), GraphicsDevice);
            renderSubject.Update(gameTime, Keyboard.GetState(), Mouse.GetState(), fpsCamera, GraphicsDevice);
            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

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
            basicEffect.View = fpsCamera.GetView();
            basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), GraphicsDevice.Viewport.AspectRatio, 0.01f, 100f);
            basicEffect.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.Enabled = true;
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.9f, 0.9f, 0.9f);
            var direction = new Vector3(2, -2, 10);
            direction.Normalize();
            basicEffect.DirectionalLight0.Direction = direction;
            if (editMode)
            {
                GraphicsDevice.SetVertexBuffer(renderSubjectLineBuffer.vertexBuffer);
                GraphicsDevice.Indices = renderSubjectLineBuffer.indexBuffer;
            }
            else
            {
                GraphicsDevice.SetVertexBuffer(renderSubjectFaceBuffer.vertexBuffer);
                GraphicsDevice.Indices = renderSubjectFaceBuffer.indexBuffer;
            }
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                if (editMode)
                {
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, renderSubjectLineBuffer.indexBuffer.IndexCount / 2);
                }
                else
                {
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, renderSubjectFaceBuffer.indexBuffer.IndexCount / 3);
                }
            }
            // draw cursor
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            _spriteBatch.Draw(cursorTexture, new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

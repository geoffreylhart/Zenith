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
        private ZGameObject renderSubject;
        private AbstractCamera camera;
        private UIContext uiContext;
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
            uiContext = new UIContext(this);
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            renderSubject = TemplateManager.Load("zdata.txt", "Spaceship1", GraphicsDevice);
            // view from slightly above and to the right, but far away TODO: for some reason we aren't looking at 0, 0, 0??
            camera = new FPSCamera(new Vector3(-2, 2, -10), new Vector3(0, 0, 0));
            uiContext.Camera = camera;
            int radii = 5;
            cursorTexture = new Texture2D(GraphicsDevice, radii * 2 + 1, radii * 2 + 1);
            Color[] data = new Color[(radii * 2 + 1) * (radii * 2 + 1)];
            for (int i = 0; i < radii * 2 + 1; i++)
            {
                data[i + (radii * 2 + 1) * radii] = Color.White; // horizontal
                data[i * (radii * 2 + 1) + radii] = Color.White; // vertical
            }
            cursorTexture.SetData(data);

            GlobalContent.Init(this.Content);

            int w = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 40;
            int h = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 40 - 45;
            _graphics.PreferredBackBufferWidth = w;
            _graphics.PreferredBackBufferHeight = h;
            Window.Position = new Point(20, 20);
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
            uiContext.UpdateGameTime(gameTime);
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || uiContext.IsKeyShiftPressed(Keys.Escape))
            {
                Exit();
            }
            if (uiContext.IsKeyCtrlPressed(Keys.S))
            {
                TemplateManager.Save(renderSubject, "zdata.txt", "Spaceship1");
            }
            if (uiContext.IsKeyCtrlPressed(Keys.E))
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
                uiContext.Camera = camera;
            }

            // TODO: Add your update logic here
            camera.Update(uiContext);
            ((MeshTemplate)renderSubject).Update(uiContext);

            uiContext.UpdateKeys();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            Matrix world = Matrix.Identity;
            Matrix view = camera.GetView();
            Matrix projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), GraphicsDevice.Viewport.AspectRatio, 0.01f, 100f);
            if (editMode)
            {
                renderSubject.DrawDebug(GraphicsDevice, world, view, projection);
            }
            else
            {
                renderSubject.Draw(GraphicsDevice, world, view, projection);
            }
            // draw cursor
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            _spriteBatch.Draw(cursorTexture, uiContext.MouseVector2, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

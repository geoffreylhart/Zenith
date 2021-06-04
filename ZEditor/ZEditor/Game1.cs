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
        private UIContext uiContext;
        private AbstractCamera camera;
        private ZGameObject mainGameObject;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D cursorTexture;

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
            uiContext = new UIContext(new InputManager(), this);
            // view from slightly above and to the right, but far away TODO: for some reason we aren't looking at 0, 0, 0??
            camera = new FPSCamera(new Vector3(-2, 2, -10), new Vector3(0, 0, 0));
            uiContext.Camera = camera;
            var renderSubject = TemplateManager.Load("zdata.txt", "Spaceship1", GraphicsDevice);
            renderSubject.Focus();
            mainGameObject = new MainGameObject(uiContext, renderSubject);
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
            ZComponent.NotifyListeners(uiContext);
            uiContext.UpdateGameTime(gameTime);

            // TODO: Add your update logic here
            camera.Update(uiContext);

            uiContext.UpdateKeys();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Matrix world = Matrix.Identity;
            Matrix view = camera.GetView();
            Matrix projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), GraphicsDevice.Viewport.AspectRatio, 0.01f, 100f);
            mainGameObject.Draw(GraphicsDevice, world, view, projection);
            // draw cursor
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            _spriteBatch.Draw(cursorTexture, uiContext.MouseVector2, Color.White);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private class MainGameObject : ZGameObject
        {
            private UIContext uiContext;
            private ZGameObject renderSubject;
            private bool editMode = false;

            public MainGameObject(UIContext uiContext, ZGameObject renderSubject)
            {
                this.uiContext = uiContext;
                this.renderSubject = renderSubject;
                RegisterListener(new InputListener(Trigger.E, x =>
                {
                    editMode = true;
                    uiContext.Camera = new EditorCamera(uiContext.Camera.GetPosition(), uiContext.Camera.GetTarget());
                    renderSubject.Focus();
                    renderSubject.RegisterListener(new InputListener(Trigger.Escape, y =>
                    {
                        this.Focus();
                        renderSubject.UnregisterListener(y);
                        editMode = false;
                        uiContext.Camera = new FPSCamera(uiContext.Camera.GetPosition(), uiContext.Camera.GetTarget());
                    }));
                }));
                RegisterGlobalListener(new InputListener(Trigger.CtrlS, x =>
                {
                    TemplateManager.Save("zdata.txt");
                }));
                RegisterListener(new InputListener(Trigger.Escape, x =>
                {
                    uiContext.Exit();
                }));
            }

            public override void Update(UIContext uiContext)
            {
                renderSubject.Update(uiContext);
            }

            public override void Draw(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
            {
                graphics.Clear(Color.CornflowerBlue);

                // TODO: Add your drawing code here
                if (editMode)
                {
                    renderSubject.DrawDebug(graphics, world, view, projection);
                }
                else
                {
                    renderSubject.Draw(graphics, world, view, projection);
                }
            }
        }
    }
}

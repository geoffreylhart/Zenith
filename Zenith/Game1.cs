using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents;

namespace Zenith
{
    public class Game1 : Game
    {
        public DebugConsole debug;
        public GraphicsDeviceManager graphics;
        public RenderTarget2D renderTarget;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            //graphics.IsFullScreen = true;
            //graphics.PreferredBackBufferWidth = 2560;
            //graphics.PreferredBackBufferHeight = 1440;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnResize;
            graphics.ApplyChanges();

            Content.RootDirectory = "Content";
        }

        private void OnResize(object sender, EventArgs e)
        {
            renderTarget.Dispose();
            renderTarget = new RenderTarget2D(
                 GraphicsDevice,
                 GraphicsDevice.Viewport.Width,
                 GraphicsDevice.Viewport.Height,
                 false,
                 GraphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
        }

        protected override void Initialize()
        {
            renderTarget = new RenderTarget2D(
                 GraphicsDevice,
                 GraphicsDevice.Viewport.Width,
                 GraphicsDevice.Viewport.Height,
                 false,
                 GraphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
            GlobalContent.Init(this.Content);

            IsMouseVisible = true;
            var camera = new EditorCamera(this);
            Components.Add(camera);
            // Components.Add(new MultiResMesh(this, camera));
            var googleMaps = new GoogleMaps(this, camera);
            var geom = new SphericalGeometryEditor(this, camera);
            Components.Add(googleMaps);
            Components.Add(geom);
            Components.Add(new UILayer(this, camera, googleMaps, geom));
            Components.Add(new CityMarker(this, camera, "Pensacola", 30.4668536, -87.3294527));
            Components.Add(new CityMarker(this, camera, "0, 0", 0, 0));
            // Components.Add(new BlenderAxis(this, camera));
            Components.Add(debug = new DebugConsole(this));
            // TODO: just change the ordering to fix this? apparantly setting a render target clears the backbuffer due to Xbox stuff
            GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            base.Draw(gameTime);
        }
    }
}

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.Helpers;
using Zenith.ZGraphics;

namespace Zenith
{
    public class Game1 : Game
    {
        public DebugConsole debug;
        public GraphicsDeviceManager graphics;
        public static RenderTarget2D renderTarget;
        public static bool recording = false;
        public int recordFrame = 0;
        public static string RECORD_PATH = @"..\..\..\..\LocalCache\Recording";

        public Game1()
        {
            Configuration.Load();
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
            //var googleMaps = new GoogleMapsSphere(this, camera);
            //var geom = new SphericalGeometryEditor(this, camera);
            var earth = new PlanetComponent(this, camera);
            //earth.Add(new GeometryEditor());
            Components.Add(earth);
            var uiLayer = new UILayer(this, new ComponentManager(camera, earth));
            Components.Add(uiLayer);
            uiLayer.UpdateOrder = camera.UpdateOrder - 1;
            //Components.Add(geom);
            Components.Add(new CityMarker(this, camera, "Pensacola", 30.4668536, -87.3294527));
            Components.Add(new CityMarker(this, camera, "Anchorage", 61.2008367, -149.8923965));
            Components.Add(new ShipComponent(this, camera));
            Components.Add(new FPSCounter(this));
            //Components.Add(new CityMarker(this, camera, "0, 0", 0, 0));
            // Components.Add(new BlenderAxis(this, camera));
            Components.Add(debug = new DebugConsole(this));
            // TODO: just change the ordering to fix this? apparantly setting a render target clears the backbuffer due to Xbox stuff
            GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            if (!Directory.Exists(RECORD_PATH)) Directory.CreateDirectory(RECORD_PATH);
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape) || Program.TERMINATE)
            {
                Exit();
            }
            if (Keyboard.GetState().WasKeyPressed(Keys.R)) recording = !recording;
            if (Keyboard.GetState().WasKeyPressed(Keys.C)) CameraMatrixManager.MODE = (CameraMatrixManager.MODE + 1) % CameraMatrixManager.MODE_COUNT;

            base.Update(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Configuration.Save();
            base.OnExiting(sender, args);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            base.Draw(gameTime);
            if (recording)
            {
                OSMSectorLoader.SuperSave(renderTarget, Path.Combine(RECORD_PATH, $"frame{recordFrame}.png"));
                recordFrame++;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.Helpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZGraphics;

namespace Zenith
{
    public class Game1 : Game
    {
        public DebugConsole debug;
        public GraphicsDeviceManager graphics;
        public static bool DEFERRED_RENDERING = true;
        public static RenderTargetBinding[] G_BUFFER;
        public static RenderTargetBinding[] RENDER_BUFFER;
        public static RenderTargetBinding[] TREE_DENSITY_BUFFER;
        public static RenderTargetBinding[] GRASS_DENSITY_BUFFER;
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
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnResize;
            graphics.ApplyChanges();

            Content.RootDirectory = "Content";
        }

        private RenderTarget2D MakeDefaultRenderTarget()
        {
            return new RenderTarget2D(
                     GraphicsDevice,
                     GraphicsDevice.Viewport.Width,
                     GraphicsDevice.Viewport.Height,
                     false,
                     GraphicsDevice.PresentationParameters.BackBufferFormat,
                     DepthFormat.Depth24);
        }

        private RenderTarget2D MakeDefaultRenderTarget(SurfaceFormat surfaceFormat)
        {
            return new RenderTarget2D(
                     GraphicsDevice,
                     GraphicsDevice.Viewport.Width,
                     GraphicsDevice.Viewport.Height,
                     false,
                     surfaceFormat,
                     DepthFormat.Depth24);
        }

        private void MakeRenderTargets()
        {
            if (DEFERRED_RENDERING)
            {
                var POSITION_BUFFER = new RenderTargetBinding(MakeDefaultRenderTarget(SurfaceFormat.Vector4)); // for now, holds the depth, TODO: why can't I just use Single? adds weird alpha
                var NORMAL_BUFFER = new RenderTargetBinding(MakeDefaultRenderTarget()); // for now, holds the normal relative to the camera (after perspective is applid)
                var ALBEDO_BUFFER = new RenderTargetBinding(MakeDefaultRenderTarget()); // holds the color
                G_BUFFER = new[] { POSITION_BUFFER, NORMAL_BUFFER, ALBEDO_BUFFER };
            }
            RENDER_BUFFER = new[] { new RenderTargetBinding(MakeDefaultRenderTarget()) };
            TREE_DENSITY_BUFFER = new[] { new RenderTargetBinding(MakeDefaultRenderTarget()) };
            GRASS_DENSITY_BUFFER = new[] { new RenderTargetBinding(MakeDefaultRenderTarget()) };
        }

        private void OnResize(object sender, EventArgs e)
        {
            foreach (var targets in new[] { G_BUFFER, RENDER_BUFFER, TREE_DENSITY_BUFFER, GRASS_DENSITY_BUFFER })
            {
                if (targets != null)
                {
                    foreach (var target in targets)
                    {
                        if (target.RenderTarget != null) target.RenderTarget.Dispose();
                    }
                }
            }
            MakeRenderTargets();
        }

        protected override void Initialize()
        {
            MakeRenderTargets();
            GlobalContent.Init(this.Content);

            IsMouseVisible = true;
            var camera = new EditorCamera(this);
            Components.Add(camera);
            var earth = new PlanetComponent(this, camera);
            Components.Add(earth);
            var uiLayer = new UILayer(this);
            Components.Add(uiLayer);
            uiLayer.UpdateOrder = camera.UpdateOrder - 1;
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
                OSMSectorLoader.SuperSave((Texture2D)RENDER_BUFFER[0].RenderTarget, Path.Combine(RECORD_PATH, $"frame{recordFrame}.png"));
                recordFrame++;
            }
        }
    }
}

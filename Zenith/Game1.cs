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
        public Effect blurHoriz;
        public Effect blurVert;
        public RenderTarget2D renderTarget;
        public RenderTarget2D renderTarget2;

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
            renderTarget2 = new RenderTarget2D(
                 GraphicsDevice,
                 GraphicsDevice.Viewport.Width,
                 GraphicsDevice.Viewport.Height,
                 false,
                 GraphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
        }

        private float[] kernel;
        private Vector2[] offsetsHoriz;
        private Vector2[] offsetsVert;
        public void ComputeKernel(int blurRadius, float blurAmount)
        {
            kernel = new float[15];
               int radius = blurRadius;
            float amount = blurAmount;

            kernel = null;
            kernel = new float[radius * 2 + 1];
            float sigma = radius / amount;

            float twoSigmaSquare = 2.0f * sigma * sigma;
            float sigmaRoot = (float)Math.Sqrt(twoSigmaSquare * Math.PI);
            float total = 0.0f;
            float distance = 0.0f;
            int index = 0;

            for (int i = -radius; i <= radius; ++i)
            {
                distance = i * i;
                index = i + radius;
                kernel[index] = (float)Math.Exp(-distance / twoSigmaSquare) / sigmaRoot;
                total += kernel[index];
            }

            for (int i = 0; i < kernel.Length; ++i)
                kernel[i] /= total;
            // do offsets
            float textureWidth = 640f;
            float textureHeight = 640f;
            offsetsHoriz = new Vector2[radius * 2 + 1];
            offsetsVert = new Vector2[radius * 2 + 1];

            int index2 = 0;
            float xOffset = 1.0f / textureWidth;
            float yOffset = 1.0f / textureHeight;

            for (int i = -radius; i <= radius; ++i)
            {
                index2 = i + radius;
                offsetsHoriz[index2] = new Vector2(i * xOffset, 0.0f);
                offsetsVert[index2] = new Vector2(0.0f, i * yOffset);
            }
        }

        protected override void Initialize()
        {
            ComputeKernel(7, 2);
            renderTarget = new RenderTarget2D(
                 GraphicsDevice,
                 GraphicsDevice.Viewport.Width,
                 GraphicsDevice.Viewport.Height,
                 false,
                 GraphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);

            blurHoriz = Content.Load<Effect>("BlurShader");
            blurVert = blurHoriz.Clone();
            blurHoriz.Parameters["weights"].SetValue(kernel);
            blurHoriz.Parameters["offsets"].SetValue(offsetsHoriz);
            blurVert.Parameters["weights"].SetValue(kernel);
            blurVert.Parameters["offsets"].SetValue(offsetsVert);
            IsMouseVisible = true;
            var camera = new EditorCamera(this);
            Components.Add(camera);
            // Components.Add(new MultiResMesh(this, camera));
            var googleMaps = new GoogleMaps(this, camera);
            var geom = new SphericalGeometryEditor(this, camera);
            Components.Add(googleMaps);
            Components.Add(geom);
            Components.Add(new ComponentManager(this, googleMaps, geom));
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

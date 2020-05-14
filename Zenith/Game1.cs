using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.Helpers;
using Zenith.LibraryWrappers.OSM;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZGame;
using Zenith.ZGraphics;
using Zenith.ZMath;
#if ANDROID
using ZenithAndroid;
#endif

namespace Zenith
{
    public class Game1 : Game
    {
        public static ISector RENDER_SECTOR = null; // if not null, render this entire sector and quit

        public List<ZGameComponent> zComponents = new List<ZGameComponent>();
        public DebugConsole debug;
        public GraphicsDeviceManager graphics;
        public static EditorCamera camera;
        public static bool DEFERRED_RENDERING = true;
        public static RenderTargetBinding[] G_BUFFER;
        public static RenderTargetBinding[] RENDER_BUFFER;
        public static RenderTargetBinding[] TREE_DENSITY_BUFFER;
        public static RenderTargetBinding[] GRASS_DENSITY_BUFFER;
        public static bool RECORDING = false;
        public static bool DEBUGGING = false;
        public int recordFrame = 0;
#if WINDOWS || LINUX
        public static string RECORD_PATH = OSMPaths.GetLocalCacheRoot() + @"\LocalCache\Recording";
#else
        public static string RECORD_PATH = "blah";
#endif

        public Game1()
        {
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

        private RenderTarget2D MakeDefaultRenderTarget(SurfaceFormat surfaceFormat, DepthFormat depthFormat)
        {
            return new RenderTarget2D(
                     GraphicsDevice,
                     GraphicsDevice.Viewport.Width,
                     GraphicsDevice.Viewport.Height,
                     false,
                     surfaceFormat,
                     depthFormat);
        }

        private void MakeRenderTargets()
        {
            if (DEFERRED_RENDERING)
            {
#if WINDOWS
                var POSITION_BUFFER = new RenderTargetBinding(MakeDefaultRenderTarget(SurfaceFormat.Vector4, DepthFormat.Depth24)); // for now, holds the depth, TODO: why can't I just use Single? adds weird alpha
                var NORMAL_BUFFER = new RenderTargetBinding(MakeDefaultRenderTarget(SurfaceFormat.Vector4, DepthFormat.Depth24)); // for now, holds the normal relative to the camera (after perspective is applid)
                var ALBEDO_BUFFER = new RenderTargetBinding(MakeDefaultRenderTarget()); // holds the color
                G_BUFFER = new[] { POSITION_BUFFER, NORMAL_BUFFER, ALBEDO_BUFFER };
#else
                var PNA_BUFFER = new RenderTargetBinding(MakeDefaultRenderTarget(SurfaceFormat.HalfVector4, DepthFormat.Depth24)); // holds everything
                G_BUFFER = new[] { PNA_BUFFER };
#endif
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
            camera = new EditorCamera(this);
            zComponents.Add(camera);
            var earth = new PlanetComponent(this, camera);
            zComponents.Add(earth);
            zComponents.Add(new CityMarker(this, camera, "Pensacola", 30.4668536, -87.3294527));
            zComponents.Add(new CityMarker(this, camera, "Anchorage", 61.2008367, -149.8923965));
#if WINDOWS || LINUX
            zComponents.Add(new ShipComponent(this, camera));
#else
            zComponents.Add(new PinchController(this, camera));
#endif
            zComponents.Add(new FPSCounter(this));
            zComponents.Add(debug = new DebugConsole(this));
            // TODO: just change the ordering to fix this? apparantly setting a render target clears the backbuffer due to Xbox stuff
            GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
#if WINDOWS || LINUX
            if (!Directory.Exists(RECORD_PATH)) Directory.CreateDirectory(RECORD_PATH);
#endif
            base.Initialize();
        }

        static int wait = 0; // wait to initialize whatever
        protected override void Update(GameTime gameTime)
        {
            if (RENDER_SECTOR != null && wait > 10)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                foreach (var child in RENDER_SECTOR.GetChildrenAtLevel(8))
                {
                    var sectorLoader = new OSMSectorLoader();
                    var buffer = sectorLoader.GetGraphicsBuffer(GraphicsDevice, child);
                    buffer.Dispose();
                }
                double secs = sw.Elapsed.TotalSeconds;
                Exit();
            }
            wait++;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape) || Constants.TERMINATE)
            {
                Exit();
            }
            if (Keyboard.GetState().WasKeyPressed(Keys.R)) RECORDING = !RECORDING;
            if (Keyboard.GetState().WasKeyPressed(Keys.T)) DEBUGGING = !DEBUGGING;
            if (Keyboard.GetState().WasKeyPressed(Keys.C)) CameraMatrixManager.MODE = (CameraMatrixManager.MODE + 1) % CameraMatrixManager.MODE_COUNT;

            foreach (var component in zComponents)
            {
                component.Update(GraphicsDevice, gameTime);
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
        }

        protected override void Draw(GameTime gameTime)
        {
            RenderContext renderContext = new RenderContext(GraphicsDevice, Matrixd.Identity(), 0, 0, 0, 0, 0, RenderContext.LayerPass.TREE_DENSITY_PASS);
            foreach (var component in zComponents)
            {
                component.InitDraw(renderContext);
            }
            GraphicsDevice.SetRenderTargets(TREE_DENSITY_BUFFER);
            DrawAllComponents(renderContext, gameTime);

            renderContext.layerPass = RenderContext.LayerPass.GRASS_DENSITY_PASS;
            GraphicsDevice.SetRenderTargets(GRASS_DENSITY_BUFFER);
            DrawAllComponents(renderContext, gameTime);

            GraphicsDevice.BlendState = DEFERRED_RENDERING ? BlendState.Opaque : BlendState.AlphaBlend;
            renderContext.layerPass = RenderContext.LayerPass.MAIN_PASS;
            GraphicsDevice.SetRenderTargets(DEFERRED_RENDERING ? G_BUFFER : RENDER_BUFFER);
            DrawAllComponents(renderContext, gameTime);
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            if (DEFERRED_RENDERING) GraphicsDevice.SetRenderTargets(RENDER_BUFFER);
            DoComposite();

            // draw UI directly to backbuffer?
            renderContext.layerPass = RenderContext.LayerPass.UI_PASS;
            DrawAllComponents(renderContext, gameTime);

            if (RECORDING)
            {
                OSMSectorLoader.SuperSave((Texture2D)RENDER_BUFFER[0].RenderTarget, Path.Combine(RECORD_PATH, $"frame{recordFrame}.png"));
                recordFrame++;
            }
        }

        private void DrawAllComponents(RenderContext renderContext, GameTime gameTime)
        {
            foreach (var component in zComponents)
            {
                component.Draw(renderContext, gameTime);
            }
        }

        private void DoComposite()
        {
            Rectangle screenRect = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);

            if (DEFERRED_RENDERING)
            {
                //GraphicsDevice.SetRenderTargets(Game1.RENDER_BUFFER);
                Matrixd projection = Matrixd.CreatePerspectiveFieldOfView(Math.PI / 4, GraphicsDevice.Viewport.AspectRatio, 0.5, 2);
                GlobalContent.SSAOShader.Parameters["PixelSize"].SetValue(new Vector2(1.0f / GraphicsDevice.Viewport.Width, 2.0f / GraphicsDevice.Viewport.Height));
                //GlobalContent.SSAOShader.Parameters["Projection"].SetValue(projection.toMatrix());
                //GlobalContent.SSAOShader.Parameters["InverseProjection"].SetValue(Matrixd.Invert(projection).toMatrix());
                Vector4[] randomOffsets = new Vector4[32];
                Random rand = new Random(12345);
                for (int i = 0; i < 32; i++)
                {
                    randomOffsets[i] = new Vector4((float)rand.NextDouble() * 2 - 1, (float)rand.NextDouble() * 2 - 1, -(float)rand.NextDouble(), 0);
                    randomOffsets[i].Normalize();
                    randomOffsets[i] = randomOffsets[i] * (float)rand.NextDouble();
                }
                GlobalContent.SSAOShader.Parameters["offsets"].SetValue(randomOffsets);
                float distance = 9 * (float)Math.Pow(0.5, Game1.camera.cameraZoom);
#if WINDOWS
                GlobalContent.SSAOShader.Parameters["AlbedoTexture"].SetValue(Game1.G_BUFFER[2].RenderTarget);
                GlobalContent.SSAOShader.Parameters["NormalTexture"].SetValue(Game1.G_BUFFER[1].RenderTarget);
                GlobalContent.SSAOShader.Parameters["PositionTexture"].SetValue(Game1.G_BUFFER[0].RenderTarget);
#else
                GlobalContent.SSAOShader.Parameters["PNATexture"].SetValue(Game1.G_BUFFER[0].RenderTarget);
#endif
                DrawSquare(GraphicsDevice, GlobalContent.SSAOShader);

                GraphicsDevice.SetRenderTarget(null);
                spriteBatch.Begin();
                spriteBatch.Draw((Texture2D)Game1.RENDER_BUFFER[0].RenderTarget, screenRect, Color.White);
                spriteBatch.End();
            }
            else
            {
                GraphicsDevice.SetRenderTarget(null);
                spriteBatch.Begin();
                spriteBatch.Draw((Texture2D)Game1.RENDER_BUFFER[0].RenderTarget, screenRect, Color.White);
                spriteBatch.End();
            }
        }

        // TODO: refactor this out
        VertexBuffer squareVertexBuffer = null;
        private void DrawSquare(GraphicsDevice graphicsDevice, Effect effect)
        {
            if (squareVertexBuffer == null)
            {
                List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
                vertices.Add(new VertexPositionTexture(new Vector3(0, 0, -10), new Vector2(0, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(1, 0, -10), new Vector2(1, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(1, 1, -10), new Vector2(1, 1)));
                vertices.Add(new VertexPositionTexture(new Vector3(0, 0, -10), new Vector2(0, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(1, 1, -10), new Vector2(1, 1)));
                vertices.Add(new VertexPositionTexture(new Vector3(0, 1, -10), new Vector2(0, 1)));
                squareVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                squareVertexBuffer.SetData(vertices.ToArray());
            }
            effect.Parameters["WVP"].SetValue(Matrix.CreateOrthographicOffCenter(0, 1, 1, 0, 1, 20));
            graphicsDevice.SetVertexBuffer(squareVertexBuffer);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            }
        }
    }
}

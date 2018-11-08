using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    internal class PlanetComponent : DrawableGameComponent, IEditorGameComponent
    {
        private double aspectRatio = 1;
        private EditorCamera camera;
        private RenderTarget2D renderTarget;
        private List<IFlatComponent> flatComponents = new List<IFlatComponent>();

        public PlanetComponent(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
            renderTarget = new RenderTarget2D(
                 GraphicsDevice,
                 512,
                 512,
                 false,
                 GraphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
        }

        public override void Draw(GameTime gameTime)
        {
            aspectRatio = GraphicsDevice.Viewport.AspectRatio;
            var basicEffect3 = this.GetDefaultEffect();
            camera.ApplyMatrices(basicEffect3);
            basicEffect3.TextureEnabled = true;
            LongLatBounds bounds = GetLongLatBounds();
            VertexIndiceBuffer sphere = SphereBuilder.MakeSphereSegExplicit(GraphicsDevice, 2, bounds.minLong, bounds.minLat, bounds.maxLong, bounds.maxLat);
            Texture2D renderToTexture = GetTexture(bounds);
            basicEffect3.Texture = renderToTexture;
            GraphicsDevice.SetRenderTarget(Game1.renderTarget);
            foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.Indices = sphere.indices;
                GraphicsDevice.SetVertexBuffer(sphere.vertices);
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
            }
            sphere.vertices.Dispose();
            sphere.indices.Dispose();
            GraphicsDevice.SetRenderTarget(null);
        }

        public override void Update(GameTime gameTime)
        {
            Vector2 mouseVector = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Vector3d circleStart = camera.GetLatLongOfCoord2(Mouse.GetState().X, Mouse.GetState().Y);
            if (circleStart != null)
            {
                Vector2 circleStart2D = new Vector2((float)circleStart.X, (float)circleStart.Y);
                // update in reverse order
                for (int i = 0; i < flatComponents.Count; i++)
                {
                    flatComponents[flatComponents.Count - 1 - i].Update(circleStart2D.X, circleStart2D.Y, camera.cameraZoom);
                }
            }
        }

        internal void Add(IFlatComponent component)
        {
            flatComponents.Add(component);
        }

        // ------------
        private double GetZoomPortion()
        {
            return Math.Pow(0.5, camera.cameraZoom) * 2;
            //return 1;
        }

        protected BasicEffect GetDefaultEffect()
        {
            var basicEffect = new BasicEffect(Game.GraphicsDevice);
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            return basicEffect;
        }
        private Texture2D GetTexture(LongLatBounds bounds)
        {
            // Set the render target
            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            // Draw the scene
            GraphicsDevice.Clear(Color.CornflowerBlue);
            BasicEffect bf = new BasicEffect(GraphicsDevice);
            bf.World = Matrix.Identity;
            //bf.World *= Matrix.CreateTranslation((float)marker.X, (float)marker.Y, (float)marker.Z);
            bf.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);
            bf.Projection = Matrix.CreateOrthographicOffCenter((float)bounds.minLong, (float)bounds.maxLong, (float)bounds.maxLat, (float)bounds.minLat, 1, 1000);

            foreach (var layer in flatComponents)
            {
                layer.Draw(GraphicsDevice, bounds.minLong, bounds.maxLong, bounds.minLat, bounds.maxLat, camera.cameraZoom);
            }

            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
            return renderTarget;
        }

        private LongLatBounds GetLongLatBounds()
        {
            // apparently we don't want to call this after changing our render target
            double w = GraphicsDevice.Viewport.Width;
            double h = GraphicsDevice.Viewport.Height;
            var leftArc = camera.GetArc(0, h, 0, 0);
            var rightArc = camera.GetArc(w, 0, w, h);
            var topArc = camera.GetArc(0, 0, w, 0);
            var bottomArc = camera.GetArc(w, h, 0, h);
            List<SphereArc> arcs = new List<SphereArc>();
            if (leftArc != null) arcs.Add(leftArc);
            if (topArc != null) arcs.Add(topArc);
            if (rightArc != null) arcs.Add(rightArc);
            if (bottomArc != null) arcs.Add(bottomArc);
            Circle3 visible = camera.GetUnitSphereVisibleCircle();
            double minLong, maxLong, minLat, maxLat;
            if (arcs.Count > 0)
            {
                int cnt = arcs.Count;
                // try and construct the arc segments that connect our disconnected arcs if they are
                for (int i = 0; i < cnt; i++)
                {
                    SphereArc arc1 = arcs[i];
                    SphereArc arc2 = arcs[(i + 1) % cnt];
                    Vector3d close1 = arc1.stop;
                    Vector3d close2 = arc2.start;
                    if ((close1 - close2).Length() > 0.01)
                    {
                        arcs.Add(new SphereArc(visible, close1, close2, true));
                    }
                }
                minLong = arcs.Min(x => x.MinLong());
                maxLong = arcs.Max(x => x.MaxLong());
                minLat = arcs.Min(x => x.MinLat());
                maxLat = arcs.Max(x => x.MaxLat());
            }
            else
            {
                minLong = visible.MinLong();
                maxLong = visible.MaxLong();
                minLat = visible.MinLat();
                maxLat = visible.MaxLat();
            }
            if (camera.IsUnitSpherePointVisible(new Vector3d(0, 0, 1)))
            {
                maxLat = Math.PI / 2;
                minLong = -Math.PI;
                maxLong = Math.PI;
            }
            if (camera.IsUnitSpherePointVisible(new Vector3d(0, 0, -1)))
            {
                minLat = -Math.PI / 2;
                minLong = -Math.PI;
                maxLong = Math.PI;
            }
            return new LongLatBounds(minLong, maxLong, minLat, maxLat);
        }

        public List<string> GetDebugInfo()
        {
            return new List<string>();
        }

        public List<IUIComponent> GetSettings()
        {
            return new List<IUIComponent>();
        }

        class LongLatBounds
        {
            public double minLong;
            public double maxLong;
            public double minLat;
            public double maxLat;

            public LongLatBounds(double minLong, double maxLong, double minLat, double maxLat)
            {
                this.minLong = minLong;
                this.maxLong = maxLong;
                this.minLat = minLat;
                this.maxLat = maxLat;
            }
        }
    }
}

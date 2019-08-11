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
using Zenith.ZGeom;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    internal class PlanetComponent : DrawableGameComponent, IEditorGameComponent
    {
        private double aspectRatio = 1;
        private EditorCamera camera;
        private Dictionary<ISector, RenderTarget2D> renderTargets = new Dictionary<ISector, RenderTarget2D>();
        private List<IFlatComponent> flatComponents = new List<IFlatComponent>();

        public PlanetComponent(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
            int sectorCount = ZCoords.GetSectorManager().GetTopmostOSMSectors().Count;
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                RenderTarget2D renderTarget = new RenderTarget2D(
                     GraphicsDevice,
                     2560 * 4 / sectorCount,
                     1440 * 4 / sectorCount,
                     true,
                     GraphicsDevice.PresentationParameters.BackBufferFormat,
                     DepthFormat.Depth24);
                renderTargets[rootSector] = renderTarget;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            aspectRatio = GraphicsDevice.Viewport.AspectRatio;
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                SectorBounds bounds = GetSectorBounds(rootSector);
                Texture2D renderToTexture = GetTexture(bounds, rootSector);
            }
            GraphicsDevice.SetRenderTarget(Game1.renderTarget);
            var basicEffect3 = this.GetDefaultEffect();
            camera.ApplyMatrices(basicEffect3);
            basicEffect3.TextureEnabled = true;
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                SectorBounds bounds = GetSectorBounds(rootSector);
                VertexIndiceBuffer sphere = SphereBuilder.MakeSphereSegExplicit(GraphicsDevice, rootSector, 2, bounds.minX, bounds.minY, bounds.maxX, bounds.maxY);
                basicEffect3.Texture = renderTargets[rootSector];
                foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.Indices = sphere.indices;
                    GraphicsDevice.SetVertexBuffer(sphere.vertices);
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
                }
                sphere.vertices.Dispose();
                sphere.indices.Dispose();
            }
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
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            return basicEffect;
        }
        private Texture2D GetTexture(SectorBounds bounds, ISector rootSector)
        {
            RenderTarget2D renderTarget = renderTargets[rootSector];
            // Set the render target
            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            // Draw the scene
            //GraphicsDevice.Clear(new[] { Color.Red, Color.Blue, Color.Yellow, Color.Green, Color.White, Color.Orange }[ZCoords.GetSectorManager().GetTopmostOSMSectors().IndexOf(rootSector)]);
            GraphicsDevice.Clear(Pallete.OCEAN_BLUE);
            BasicEffect bf = new BasicEffect(GraphicsDevice);
            bf.World = Matrix.Identity;
            //bf.World *= Matrix.CreateTranslation((float)marker.X, (float)marker.Y, (float)marker.Z);
            bf.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);
            bf.Projection = Matrix.CreateOrthographicOffCenter((float)bounds.minX, (float)bounds.maxX, (float)bounds.maxY, (float)bounds.minY, 1, 1000);

            foreach (var layer in flatComponents)
            {
                layer.Draw(renderTarget, rootSector, bounds.minX, bounds.maxX, bounds.minY, bounds.maxY, camera.cameraZoom);
            }

            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
            return renderTarget;
        }

        private SectorBounds GetSectorBounds(ISector rootSector)
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
            double minX, maxX, minY, maxY;
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
                minX = arcs.Min(x => x.Min(y => rootSector.ProjectToLocalCoordinates(y).X));
                maxX = arcs.Max(x => x.Max(y => rootSector.ProjectToLocalCoordinates(y).X));
                minY = arcs.Min(x => x.Min(y => rootSector.ProjectToLocalCoordinates(y).Y));
                maxY = arcs.Max(x => x.Max(y => rootSector.ProjectToLocalCoordinates(y).Y));
            }
            else
            {
                minX = visible.Min(x => rootSector.ProjectToLocalCoordinates(x).X);
                maxX = visible.Max(x => rootSector.ProjectToLocalCoordinates(x).X);
                minY = visible.Min(x => rootSector.ProjectToLocalCoordinates(x).Y);
                maxY = visible.Max(x => rootSector.ProjectToLocalCoordinates(x).Y);
            }
            if (rootSector is MercatorSector)
            {
                if (camera.IsUnitSpherePointVisible(new Vector3d(0, 0, 1)))
                {
                    minY = 0;
                    minX = 0;
                    maxX = 1;
                }
                if (camera.IsUnitSpherePointVisible(new Vector3d(0, 0, -1)))
                {
                    maxY = 1;
                    minX = 0;
                    maxX = 1;
                }
            }
            return new SectorBounds(Math.Max(minX, 0), Math.Min(maxX, 1), Math.Max(minY, 0), Math.Min(maxY, 1));
        }

        public List<string> GetDebugInfo()
        {
            return new List<string>();
        }

        public List<IUIComponent> GetSettings()
        {
            return new List<IUIComponent>();
        }

        public List<IEditorGameComponent> GetSubComponents()
        {
            return flatComponents.Where(x => x is IEditorGameComponent).Cast<IEditorGameComponent>().ToList();
        }

        class SectorBounds
        {
            public double minX;
            public double maxX;
            public double minY;
            public double maxY;

            public SectorBounds(double minX, double maxX, double minY, double maxY)
            {
                this.minX = minX;
                this.maxX = maxX;
                this.minY = minY;
                this.maxY = maxY;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;

namespace Zenith.EditorGameComponents
{
    internal class PlanetComponent : EditorGameComponent
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
            VertexIndiceBuffer sphere = SphereBuilder.MakeSphereSegExplicit(GraphicsDevice, 2, GetMinLong(), GetMinLat(), GetMaxLong(), GetMaxLat());
            Texture2D renderToTexture = GetTexture();
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
                foreach (var layer in flatComponents)
                {
                    layer.Update(circleStart2D.X, circleStart2D.Y, camera.cameraZoom);
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
        private Texture2D GetTexture()
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
            double minLong = GetMinLong();
            double maxLong = GetMaxLong();
            double minLat = GetMinLat();
            double maxLat = GetMaxLat();
            bf.Projection = Matrix.CreateOrthographicOffCenter((float)(minLong), (float)(maxLong), (float)maxLat, (float)minLat, 1, 1000);

            foreach (var layer in flatComponents)
            {
                layer.Draw(GraphicsDevice, minLong, maxLong, minLat, maxLat);
            }

            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
            return renderTarget;
        }

        private double GetMinLong()
        {
            double longi = camera.cameraRotX;
            double lat = camera.cameraRotY;
            LongLatHelper.NormalizeLongLatRadians(ref longi, ref lat);
            return longi - Math.PI * Math.Pow(0.5, camera.cameraZoom) * 3 * aspectRatio;
        }

        private double GetMaxLong()
        {
            double longi = camera.cameraRotX;
            double lat = camera.cameraRotY;
            LongLatHelper.NormalizeLongLatRadians(ref longi, ref lat);
            return longi + Math.PI * Math.Pow(0.5, camera.cameraZoom) * 3 * aspectRatio;
        }

        private double GetMinLat()
        {
            double longi = camera.cameraRotX;
            double lat = camera.cameraRotY;
            LongLatHelper.NormalizeLongLatRadians(ref longi, ref lat);
            return Math.Max(lat - Math.Pow(0.5, camera.cameraZoom) * 3 * Math.PI, -Math.PI / 2);
        }

        private double GetMaxLat()
        {
            double longi = camera.cameraRotX;
            double lat = camera.cameraRotY;
            LongLatHelper.NormalizeLongLatRadians(ref longi, ref lat);
            return Math.Min(lat + Math.Pow(0.5, camera.cameraZoom) * 3 * Math.PI, Math.PI / 2);
        }
    }
}

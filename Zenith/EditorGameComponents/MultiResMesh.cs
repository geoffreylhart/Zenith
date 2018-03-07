using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;

namespace Zenith.EditorGameComponents
{
    public class MultiResMesh : DrawableGameComponent
    {
        private EditorCamera camera;
        List<List<VertexPositionColor>> circleLatLong = new List<List<VertexPositionColor>>();
        int LL_SEGMENTS = 3; // we'll split it up into the mininum number of longitude slices to keep things smoothly cycling
        RenderTarget2D renderTarget;
        private double traceAlpha = 0.5;

        public MultiResMesh(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
            for (int i = 0; i < LL_SEGMENTS; i++) circleLatLong.Add(new List<VertexPositionColor>());
        }

        public override void Initialize()
        {
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
            var basicEffect3 = this.GetDefaultEffect();
            camera.ApplyMatrices(basicEffect3);
            basicEffect3.TextureEnabled = true;
            VertexIndiceBuffer sphere = SphereBuilder.MakeSphereSeg(GraphicsDevice, 2, GetZoomPortion(), camera.cameraRotY, camera.cameraRotX);
            Texture2D renderToTexture = GetTexture(sphere);
            basicEffect3.Texture = renderToTexture;
            basicEffect3.Alpha = (float)traceAlpha;
            foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.Indices = sphere.indices;
                GraphicsDevice.SetVertexBuffer(sphere.vertices);
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
            }
            sphere.vertices.Dispose();
            sphere.indices.Dispose();
            var basicEffect2 = this.GetDefaultEffect();
            camera.ApplyMatrices(basicEffect2);
            foreach (var building in buildings)
            {
                foreach (EffectPass pass in basicEffect2.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.Indices = building.indices;
                    GraphicsDevice.SetVertexBuffer(building.vertices);
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, building.indices.IndexCount / 3);
                }
            }
        }

        private double GetZoomPortion()
        {
            return Math.Pow(0.5, camera.cameraZoom) * 2;
            //return 1;
        }

        public override void Update(GameTime gameTime)
        {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed) MakeThatCircle(8, 10);
            if (Mouse.GetState().WasRightPressed()) MakeABuilding();
            Keyboard.GetState().AffectNumber(ref traceAlpha, Keys.OemMinus, Keys.OemPlus, 0.01, 0, 1);
        }

        List<VertexIndiceBuffer> buildings = new List<VertexIndiceBuffer>();
        private void MakeABuilding()
        {
            Rayd ray = camera.CastFromCamera(new Vector2(Mouse.GetState().X, Mouse.GetState().Y));
            Vector3d intersection = ray.IntersectionSphere(new Vector3d(0, 0, 0), 1); // angle 0
            if (intersection != null) buildings.Add(CubeBuilder.MakeBasicBuildingCube(GraphicsDevice, intersection.ToVector3()));
            //buildings.Add(CubeBuilder.MakeBasicCube(GraphicsDevice));
        }

        private void MakeThatCircle(int circRez, double circR)
        {
            circR = Math.Pow(0.5, camera.cameraZoom) / 4;
            Vector2 mouseVector = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            //Vector3d circleStart = camera.GetLatLongOfCoord(mouseVector + new Vector2((float)circR, 0));
            Vector3d circleStart = camera.GetLatLongOfCoord2(Mouse.GetState().X, Mouse.GetState().Y);
            // ((Game1)this.Game).debug.DebugSet(ToLatLong(circleStart));
            if (circleStart == null) return;
            List<VertexPositionColor> tempLatLong = new List<VertexPositionColor>();
            for (int i = 1; i < circRez; i++)
            {
                double angle1 = Math.PI * 2 / circRez * i;
                double angle2 = Math.PI * 2 / circRez * (i + 1);
                tempLatLong.Add(new VertexPositionColor((circleStart + new Vector3d(circR, 0, 0)).ToVector3(), Color.Green));
                tempLatLong.Add(new VertexPositionColor((circleStart + new Vector3d(Math.Cos(angle1) * circR, Math.Sin(angle1) * circR, 0)).ToVector3(), Color.Green));
                tempLatLong.Add(new VertexPositionColor((circleStart + new Vector3d(Math.Cos(angle2) * circR, Math.Sin(angle2) * circR, 0)).ToVector3(), Color.Green));
            }
            // yup, we'll have trouble with huge triangles in the future
            circleLatLong[(int)((circleStart.X + Math.PI) / (2 * Math.PI) * LL_SEGMENTS)].AddRange(tempLatLong);
        }

        private Texture2D GetTexture(VertexIndiceBuffer sphere)
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

            double lat = camera.cameraRotY; // same as for when generating the sphere
            double longi = camera.cameraRotX; // same as for when generating the sohere etc
            double portion = GetZoomPortion(); // same as for when generating the sphere
            // literally copy-pasted
            double minLat = Math.Max(lat - portion * Math.PI, -Math.PI / 2);
            double maxLat = Math.Min(lat + portion * Math.PI, Math.PI / 2);
            double minLong = longi - Math.PI * portion;
            double maxLong = longi + Math.PI * portion;


            GraphicsDevice.Indices = sphere.indices;
            GraphicsDevice.SetVertexBuffer(sphere.vertices);
            for (int i = 0; i < LL_SEGMENTS; i++)
            {
                double segmin = i * 2 * Math.PI / LL_SEGMENTS - Math.PI;
                double segmax = (i + 1) * 2 * Math.PI / LL_SEGMENTS - Math.PI;
                double offset = 0;
                // try to get the two ranges to overlap (doesnt always happen)
                while (minLong + offset > segmax)
                {
                    offset -= 2 * Math.PI;
                }
                while (maxLong + offset < segmin)
                {
                    offset += 2 * Math.PI;
                }
                bf.Projection = Matrix.CreateOrthographicOffCenter((float)(minLong + offset), (float)(maxLong + offset), (float)maxLat, (float)minLat, 1, 1000);
                var section = circleLatLong[i];
                if (section.Count >= 3)
                {
                    foreach (EffectPass pass in bf.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, section.ToArray(), 0, section.Count / 3);
                    }
                }
            }
            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
            return renderTarget;
        }
    }
}

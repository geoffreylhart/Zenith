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
        // temporary, not copied from pensacola, but traced
        Vector3d marker = new Vector3d(-1.52420012889899, -0.531647256354102, 0);
        //Vector3d marker = new Vector3d(0, 0, 0);
        private EditorCamera camera;
        List<VertexPositionColor> circleLatLong = new List<VertexPositionColor>();
        RenderTarget2D renderTarget;
        private double traceAlpha = 0.5;

        public MultiResMesh(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
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

        VertexIndiceBuffer sphere;
        public override void Draw(GameTime gameTime)
        {
            var basicEffect3 = MakeThatBasicEffect3();
            basicEffect3.TextureEnabled = true;
            Texture2D renderToTexture = GetTexture();
            sphere = SphereBuilder.MakeSphereSeg(GraphicsDevice, 2, GetZoomPortion(), camera.cameraRotY, camera.cameraRotX);
            basicEffect3.Texture = renderToTexture;
            basicEffect3.Alpha = (float)traceAlpha;
            foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.Indices = sphere.indices;
                GraphicsDevice.SetVertexBuffer(sphere.vertices);
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
            }
            var basicEffect2 = MakeThatBasicEffect3();
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
            return Math.Pow(0.5, camera.cameraZoom);
            //return 1;
        }

        public override void Update(GameTime gameTime)
        {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed) MakeThatCircle(8, 10, MakeThatBasicEffect3());
            if (Mouse.GetState().WasRightPressed()) MakeABuilding();
            Keyboard.GetState().AffectNumber(ref traceAlpha, Keys.OemMinus, Keys.OemPlus, 0.01, 0, 1);
        }

        List<VertexIndiceBuffer> buildings = new List<VertexIndiceBuffer>();
        private void MakeABuilding()
        {
            Vector3d buildingCenter = camera.GetLatLongOfCoord2(Mouse.GetState().X, Mouse.GetState().Y);
            //if (buildingCenter != null) buildings.Add(CubeBuilder.MakeBasicBuildingCube(GraphicsDevice, buildingCenter.X, buildingCenter.Y));
            buildings.Add(CubeBuilder.MakeBasicCube(GraphicsDevice));
        }

        private void MakeThatCircle(int circRez, double circR, BasicEffect basicEffect3)
        {
            circR = Math.Pow(0.5, camera.cameraZoom) / 4;
            Vector2 mouseVector = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            //Vector3d circleStart = camera.GetLatLongOfCoord(mouseVector + new Vector2((float)circR, 0));
            Vector3d circleStart = camera.GetLatLongOfCoord(new Vector2(Mouse.GetState().X, Mouse.GetState().Y));
            // ((Game1)this.Game).debug.DebugSet(ToLatLong(circleStart));
            if (circleStart == null) return;
            List<VertexPositionColor> tempLatLong = new List<VertexPositionColor>();
            // 3d code, temporary
            // 2d code
            for (int i = 1; i < circRez; i++)
            {
                double angle1 = Math.PI * 2 / circRez * i;
                double angle2 = Math.PI * 2 / circRez * (i + 1);
                //Vector3d off1 = camera.GetLatLongOfCoord(mouseVector + new Vector2((float)(Math.Cos(angle1) * circR), (float)(Math.Sin(angle1) * circR)));
                //Vector3d off2 = camera.GetLatLongOfCoord(mouseVector + new Vector2((float)(Math.Cos(angle2) * circR), (float)(Math.Sin(angle2) * circR)));
                //if (off1 == null || off2 == null) return;
                //tempLatLong.Add(new VertexPositionColor(circleStart.ToVector3(), Color.Green));
                //tempLatLong.Add(new VertexPositionColor(off1.ToVector3(), Color.Green));
                //tempLatLong.Add(new VertexPositionColor(off2.ToVector3(), Color.Green));
                tempLatLong.Add(new VertexPositionColor((circleStart + new Vector3d(circR, 0, 0)).ToVector3(), Color.Green));
                tempLatLong.Add(new VertexPositionColor((circleStart + new Vector3d(Math.Cos(angle1) * circR, Math.Sin(angle1) * circR, 0)).ToVector3(), Color.Green));
                tempLatLong.Add(new VertexPositionColor((circleStart + new Vector3d(Math.Cos(angle2) * circR, Math.Sin(angle2) * circR, 0)).ToVector3(), Color.Green));
            }
            circleLatLong.AddRange(tempLatLong);
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

            double lat = camera.cameraRotY; // same as for when generating the sphere
            double longi = camera.cameraRotX; // same as for when generating the sohere etc
            double portion = GetZoomPortion(); // same as for when generating the sphere
            // literally copy-pasted
            double minLat = Math.Max(lat - portion * Math.PI, -Math.PI / 2);
            double maxLat = Math.Min(lat + portion * Math.PI, Math.PI / 2);
            double minLong = longi - Math.PI * portion;
            double maxLong = longi + Math.PI * portion;

            bf.Projection = Matrix.CreateOrthographicOffCenter((float)minLong, (float)maxLong, (float)maxLat, (float)minLat, 1, 1000);

            if (circleLatLong.Count >= 3)
            {
                foreach (EffectPass pass in bf.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.Indices = sphere.indices;
                    GraphicsDevice.SetVertexBuffer(sphere.vertices);
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, circleLatLong.ToArray(), 0, circleLatLong.Count / 3);
                }
            }
            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
            return renderTarget;
        }

        private BasicEffect MakeThatBasicEffect3()
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.LightingEnabled = true;
            basicEffect3.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            basicEffect3.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect3.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            camera.ApplyMatrices(basicEffect3);
            return basicEffect3;
        }

        // output ranged from -PI/2 to PI/2
        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }

        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }
        
        private Vector3d ToLatLong(Vector3d v)
        {
            return new Vector3d(Math.Atan2(v.X, -v.Y), Math.Asin(v.Z), 0);
        }
    }
}

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
            sphere = SphereBuilder.MakeSphereSeg(GraphicsDevice, 2, GetZoomPortion(), -camera.cameraRotY, -camera.cameraRotX + Math.PI / 2);
            basicEffect3.Texture = renderToTexture;
            basicEffect3.Alpha = (float)traceAlpha;
            foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.Indices = sphere.indices;
                GraphicsDevice.SetVertexBuffer(sphere.vertices);
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
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
            Keyboard.GetState().AffectNumber(ref traceAlpha, Keys.OemMinus, Keys.OemPlus, 0.01, 0, 1);
        }

        private void MakeThatCircle(int circRez, double circR, BasicEffect basicEffect3)
        {
            Vector2 mouseVector = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            Ray mouseRay = RayHelper.CastFromCamera(GraphicsDevice, mouseVector + new Vector2((float)circR, 0), basicEffect3.Projection, basicEffect3.View, basicEffect3.World);
            Vector3? circleStart = mouseRay.IntersectionSphere(new BoundingSphere(new Vector3(0, 0, 0), 1)); // angle 0
            if (!circleStart.HasValue) return;
            List<VertexPositionColor> tempLatLong = new List<VertexPositionColor>();
            // 3d code, temporary
            // 2d code
            for (int i = 0; i < circRez; i++)
            {
                double angle1 = Math.PI * 2 / circRez * i;
                double angle2 = Math.PI * 2 / circRez * (i + 1);
                Vector2 off1 = mouseVector + new Vector2((float)(Math.Cos(angle1) * circR), (float)(Math.Sin(angle1) * circR));
                Vector2 off2 = mouseVector + new Vector2((float)(Math.Cos(angle2) * circR), (float)(Math.Sin(angle2) * circR));
                Ray angle1Ray1 = RayHelper.CastFromCamera(GraphicsDevice, off1, basicEffect3.Projection, basicEffect3.View, basicEffect3.World);
                Ray angle1Ray2 = RayHelper.CastFromCamera(GraphicsDevice, off2, basicEffect3.Projection, basicEffect3.View, basicEffect3.World);
                Vector3? onSphere1 = angle1Ray1.IntersectionSphere(new BoundingSphere(new Vector3(0, 0, 0), 1));
                Vector3? onSphere2 = angle1Ray2.IntersectionSphere(new BoundingSphere(new Vector3(0, 0, 0), 1));
                if (!onSphere1.HasValue || !onSphere2.HasValue) return;
                tempLatLong.Add(new VertexPositionColor(ToLatLong(circleStart.Value), Color.Green));
                tempLatLong.Add(new VertexPositionColor(ToLatLong(onSphere1.Value), Color.Green));
                tempLatLong.Add(new VertexPositionColor(ToLatLong(onSphere2.Value), Color.Green));
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
            bf.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);

            double lat = -camera.cameraRotY; // same as for when generating the sphere
            double longi = -camera.cameraRotX + Math.PI / 2; // same as for when generating the sohere etc
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
            basicEffect3.DirectionalLight0.Direction = new Vector3(1, -1, 0);
            basicEffect3.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect3.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            //basicEffect3.World = Matrix.CreateRotationX((float)cameraRotY) * Matrix.CreateRotationZ((float)cameraRotX);
            basicEffect3.World = Matrix.CreateRotationZ((float)camera.cameraRotX) * Matrix.CreateRotationX((float)camera.cameraRotY);
            float distance = (float)(Math.Pow(0.5, camera.cameraZoom) * 19);
            // TODO: cheated here and just turned the camera upside down
            basicEffect3.View = Matrix.CreateLookAt(new Vector3(0, -20, 0), new Vector3(0, 0, 0), -Vector3.UnitZ); // we'll match Blender for "up" of camera and camera starting position
            // TODO: I just have no clue why the camera isn't working like it used to where I just move the camera closer and closer
            basicEffect3.Projection = Matrix.CreatePerspectiveFieldOfView((float)((Math.PI / 4) * Math.Pow(0.5, camera.cameraZoom)), 800f / 480f, 0.1f, 100); // was 0.1f and 100f
            //Debug(-distance - 1f);
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

        // latitude should be y, just makes sense, right?
        private Vector3 ToLatLong(Vector3 v)
        {
            return new Vector3((float)Math.Atan2(v.Y, v.X), (float)Math.Asin(v.Z), 0);
        }
    }
}

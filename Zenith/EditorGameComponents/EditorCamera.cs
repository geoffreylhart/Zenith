using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;
using Zenith.MathHelpers;

namespace Zenith.EditorGameComponents
{
    public class EditorCamera : GameComponent, IUpdateable
    {
        public double cameraRotX = 0; // longitude coordinate of our character
        public double cameraRotY = 0; // latitude coordinate of our character
        public double cameraZoom = 1; // no basis in reality (YET), positive means increased zoom, though
        private Matrix world;
        private Matrix view;
        private Matrix projection;

        public EditorCamera(Game game) : base(game)
        {
        }

        public override void Update(GameTime gameTime)
        {
            this.GetDebugConsole().DebugSet(cameraRotX + ":" + cameraRotY + ":" + cameraZoom);
            double cameraMoveAmount = 0.05 * Math.Pow(0.5, cameraZoom);
            Keyboard.GetState().AffectNumber(ref cameraRotX, Keys.Left, Keys.Right, Keys.A, Keys.D, cameraMoveAmount);
            Keyboard.GetState().AffectNumber(ref cameraRotY, Keys.Down, Keys.Up, Keys.S, Keys.W, cameraMoveAmount);
            Keyboard.GetState().AffectNumber(ref cameraZoom, Keys.Space, Keys.LeftShift, 0.01);
            world = Matrix.CreateRotationZ(-(float)cameraRotX) * Matrix.CreateRotationX((float)cameraRotY); // eh.... think hard on this later
            float distance = (float)(9 * Math.Pow(0.5, cameraZoom));
            view = Matrix.CreateLookAt(new Vector3(0, -1 - distance, 0), new Vector3(0, 0, 0), Vector3.UnitZ);
            projection = Matrix.CreatePerspectiveFieldOfView(Mathf.PI / 2, GetAspectRatio(), distance * 0.1f, distance * 100);
        }

        private float GetAspectRatio()
        {
            return this.Game.GraphicsDevice.Viewport.AspectRatio;
        }

        internal void ApplyMatrices(BasicEffect basicEffect)
        {
            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;
        }

        internal Vector3 Project(Vector3 v)
        {
            return Game.GraphicsDevice.Viewport.Project(v, projection, view, world);
        }

        internal Vector3 Unproject(Vector3 v)
        {
            return Game.GraphicsDevice.Viewport.Unproject(v, projection, view, world);
        }

        // yup, it returns lat/long only in the range you'd expect
        internal Vector3d GetLatLongOfCoord(Vector2 mouseVector)
        {
            Rayd ray = Rayd.CastFromCamera(Game.GraphicsDevice, mouseVector, projection, view, world);
            Vector3d intersection = ray.IntersectionSphere(new Vector3d(0, 0, 0), 1); // angle 0
            if (intersection == null) return null;
            return ToLatLong(intersection);
        }

        internal Rayd CastFromCamera(Vector2 mouseVector)
        {
            Matrixd worldd = Matrixd.CreateRotationZ(-cameraRotX) * Matrixd.CreateRotationX(cameraRotY);
            float distance = (float)(9 * Math.Pow(0.5, cameraZoom));
            Matrixd viewd = Matrixd.CreateLookAt(new Vector3d(0, -1 - distance, 0), new Vector3d(0, 0, 0), new Vector3d(0, 0, 1));
            Matrixd projectiond = Matrixd.CreatePerspectiveFieldOfView(Mathf.PI / 2, GetAspectRatio(), distance * 0.1f, distance * 100);
            return Rayd.CastFromCamera2(Game.GraphicsDevice, mouseVector.X, mouseVector.Y, projectiond, viewd, worldd);
        }

        // TODO: probably keep all of the double precision classes, but discard stuff I've written myself

        // more accurate version
        internal Vector3d GetLatLongOfCoord2(double x, double y)
        {
            Matrixd worldd = Matrixd.CreateRotationZ(-cameraRotX) * Matrixd.CreateRotationX(cameraRotY);
            float distance = (float)(9 * Math.Pow(0.5, cameraZoom));
            Matrixd viewd = Matrixd.CreateLookAt(new Vector3d(0, -1 - distance, 0), new Vector3d(0, 0, 0), new Vector3d(0, 0, 1));
            Matrixd projectiond = Matrixd.CreatePerspectiveFieldOfView(Mathf.PI / 2, GetAspectRatio(), distance * 0.1f, distance * 100);
            Rayd ray = Rayd.CastFromCamera2(Game.GraphicsDevice, x, y, projectiond, viewd, worldd);
            Vector3d intersection = ray.IntersectionSphere(new Vector3d(0, 0, 0), 1);
            if (intersection == null) return null;
            return ToLatLong(intersection);
        }

        private static bool WithinEpsilon(double a, double b)
        {
            double num = a - b;
            return ((-1.401298E-45f <= num) && (num <= double.Epsilon));
        }

        private double getFOV()
        {
            return Math.PI / 4 * Math.Pow(0.5, cameraZoom);
        }

        private Vector3d ToLatLong(Vector3d v)
        {
            return new Vector3d(Math.Atan2(v.X, -v.Y), Math.Asin(v.Z), 0);
        }
    }
}

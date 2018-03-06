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
            projection = Matrix.CreatePerspectiveFieldOfView(Mathf.PI / 2, 800f / 480f, distance * 0.1f, distance * 100);
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
            Matrixd projectiond = Matrixd.CreatePerspectiveFieldOfView(Mathf.PI / 2, 800f / 480f, distance * 0.1f, distance * 100);
            return Rayd.CastFromCamera2(Game.GraphicsDevice, mouseVector.X, mouseVector.Y, projectiond, viewd, worldd);
        }

        // TODO: probably keep all of the double precision classes, but discard stuff I've written myself

        // more accurate version
        internal Vector3d GetLatLongOfCoord2(double x, double y)
        {
            Matrixd worldd = Matrixd.CreateRotationZ(-cameraRotX) * Matrixd.CreateRotationX(cameraRotY);
            float distance = (float)(9 * Math.Pow(0.5, cameraZoom));
            Matrixd viewd = Matrixd.CreateLookAt(new Vector3d(0, -1 - distance, 0), new Vector3d(0, 0, 0), new Vector3d(0, 0, 1));
            Matrixd projectiond = Matrixd.CreatePerspectiveFieldOfView(Mathf.PI / 2, 800f / 480f, distance * 0.1f, distance * 100);
            Rayd ray = Rayd.CastFromCamera2(Game.GraphicsDevice, x, y, projectiond, viewd, worldd);
            Vector3d intersection = ray.IntersectionSphere(new Vector3d(0, 0, 0), 1);
            if (intersection == null) return null;
            return ToLatLong(intersection);
        }

        // simplified version
        internal Vector3d GetLatLongOfCoord3(double x, double y)
        {
            Matrixd worldd = Matrixd.Identity();
            Matrixd viewd = new Matrixd(-1, 0, 0, 0, 0, 0, -2, 0, 0, -1, 0, 0, 0, 0, -20, 1);
            double num = 1f / Math.Tan(getFOV() * 0.5);
            double num9 = num * 480 / 800;
            Matrixd projectiond = new Matrixd(num9, 0, 0, 0, 0, num, 0, 0, 0, 0, -100 / 99.9, -1, 0, 0, -10 / 99.9, 0);
            //Vector3d unprojected = Matrixd.Unproject(Game.GraphicsDevice.Viewport, new Vector3d(x, y, 0), projectiond, viewd, worldd);
            Matrixd matrix = Matrixd.Invert(Matrixd.Multiply(Matrixd.Multiply(worldd, viewd), projectiond));
            Vector3d source2 = new Vector3d(x / 400 - 1, 1 - y / 480, 0);
            double a = (((source2.X * matrix.M14) + (source2.Y * matrix.M24)) + (source2.Z * matrix.M34)) + matrix.M44;
            Vector3d unprojected = Vector3d.Transform(source2, matrix) / a;
            Vector3d unprojected2 = Matrixd.Unproject(Game.GraphicsDevice.Viewport, new Vector3d(x, y, 1), projectiond, viewd, worldd);
            Rayd ray = new Rayd(unprojected2, unprojected - unprojected2);
            //Rayd ray = Rayd.CastFromCamera2(Game.GraphicsDevice, x, y, projectiond, viewd, worldd);
            Vector3d intersection = ray.IntersectionSphere(new Vector3d(0, 0, 0), 1); // angle 0
            if (intersection == null) return null;
            return ToLatLong(intersection) + new Vector3d(-cameraRotX, -cameraRotY, 0);
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

        // better precision version, but not super generic
        //internal Vector3d GetLatLongOfCoord2(Vector2 mouseVector)
        //{
        //    // we can view this as 2d to keep the math ultra-simple, in our special case
        //    Vector2 offset = mouseVector - new Vector2(400, 240);


        //    double angle = offset.Length() / 480 * getFOV();
        //    Rayd ray = Rayd.CastFromCamera(Game.GraphicsDevice, mouseVector, projection, view, world);
        //    Rayd ray2 = Rayd.CastFromCamera(Game.GraphicsDevice, new Vector2(400, 240), projection, view, world);
        //    double actualAngle = Math.Acos(Vector3d.Dot(ray.Direction, ray2.Direction)/ray.Direction.Length()/ray2.Direction.Length());
        //    ((Game1)this.Game).debug.DebugSet(actualAngle/angle);



        //    // y=sin(angle)x
        //    // y^2+(x-20)^2=1
        //    // sin(angle)^2x^2=1-(x-20)^2
        //    // sin(angle)^2x^2=1-x^2+40x-400
        //    // (sin(angle)^2+1)x^2-40x+399=0
        //    //if (b * b - 4 * a * c < 0) return null; // 4
        //    double angleMoved = Special(offset.Length());
        //    // this might not be correct, but let's try this math for converting to lat/long difference
        //    // return new Vector3d(-cameraRotX+Math.PI/2 , -cameraRotY , 0); ugh, came by this by guessing, no good, really have to refactor things
        //    return new Vector3d(-cameraRotX+offset.X/offset.Length()*angleMoved + Math.PI/2, -cameraRotY+offset.Y / offset.Length() * angleMoved, 0);
        //}

        //// turn pixel offset into angle offset or w/e
        //private double Special(double x)
        //{
        //    double angle = x / 480 * getFOV();
        //    double a = Math.Sin(angle) * Math.Sin(angle) + 1; // 1
        //    double b = -40;
        //    double c = 399;
        //    double x2 = (-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
        //    double y = Math.Sin(angle) * x2;
        //    return Math.Asin(y); // bleh, why times 1.85?
        //}

        private Vector3d ToLatLong(Vector3d v)
        {
            return new Vector3d(Math.Atan2(v.X, -v.Y), Math.Asin(v.Z), 0);
        }
    }
}

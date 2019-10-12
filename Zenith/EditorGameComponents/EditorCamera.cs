using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    public class EditorCamera : DrawableGameComponent, IEditorGameComponent
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

        public void UpdateCamera()
        {
            world = Matrix.CreateRotationZ(-(float)cameraRotX) * Matrix.CreateRotationX((float)cameraRotY); // eh.... think hard on this later
            float distance = (float)(9 * Math.Pow(0.5, cameraZoom));
            view = CameraMatrixManager.GetWorldView(distance);
            projection = CameraMatrixManager.GetWorldProjection(distance, this.GraphicsDevice.Viewport.AspectRatio);
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
        internal Vector3d GetLatLongOfCoord(double x, double y)
        {
            Matrixd worldd = Matrixd.CreateRotationZ(-cameraRotX) * Matrixd.CreateRotationX(cameraRotY);
            double distance = 9 * Math.Pow(0.5, cameraZoom);
            Matrixd viewd = CameraMatrixManager.GetWorldViewd(distance);
            Matrixd projectiond = CameraMatrixManager.GetWorldProjectiond(distance, this.GraphicsDevice.Viewport.AspectRatio);

            Rayd ray = Rayd.CastFromCamera(Game.GraphicsDevice, x, y, projectiond, viewd, worldd);
            Vector3d intersection = ray.IntersectionSphere(new Vector3d(0, 0, 0), 1); // angle 0
            if (intersection == null) return null;
            return ToLatLong(intersection);
        }

        internal Rayd CastFromCamera(Vector2 mouseVector)
        {
            Matrixd worldd = Matrixd.CreateRotationZ(-cameraRotX) * Matrixd.CreateRotationX(cameraRotY);
            double distance = 9 * Math.Pow(0.5, cameraZoom);
            Matrixd viewd = CameraMatrixManager.GetWorldViewd(distance);
            Matrixd projectiond = CameraMatrixManager.GetWorldProjectiond(distance, this.GraphicsDevice.Viewport.AspectRatio);
            return Rayd.CastFromCamera(Game.GraphicsDevice, mouseVector.X, mouseVector.Y, projectiond, viewd, worldd);
        }

        internal Vector3d GetUnitSphereIntersection(double x, double y)
        {
            var latlong = GetLatLongOfCoord(x, y);
            return latlong == null ? null : To3D(latlong);
        }

        internal static Vector3d To3D(Vector3d longLat)
        {
            double dz = Math.Sin(longLat.Y);
            double dxy = Math.Cos(longLat.Y); // the radius of the horizontal ring section, always positive
            double dx = Math.Sin(longLat.X) * dxy;
            double dy = -Math.Cos(longLat.X) * dxy;
            return new Vector3d(dx, dy, dz);
        }

        internal SphereArc GetArc(double x1, double y1, double x2, double y2)
        {
            Vector3 across = Unproject(new Vector3((float)x2, (float)y2, 1)) - Unproject(new Vector3((float)x1, (float)y1, 1));
            Vector3d into = CastFromCamera(new Vector2((float)x1, (float)y1)).Direction; // any vector along our slice that points away from the camera
            Vector3d normal = Vector3d.Cross(new Vector3d(across), into);
            Vector3d center = CastFromCamera(new Vector2((float)x1, (float)y1)).Position;
            var plane = new ZMath.Plane(center, normal);
            Circle3 intersection = plane.GetUnitSphereIntersection();
            if (intersection == null) return null;
            Vector3d[] tangents = intersection.GetTangents(GetPosition());
            Vector3[] projected = tangents.Select(x => Project(x.ToVector3())).ToArray();
            Vector3d xy1 = GetUnitSphereIntersection(x1, y1);
            Vector3d xy2 = GetUnitSphereIntersection(x2, y2);
            // TODO: correctly assign start/stop (I think our current logic works for our uses)
            if ((projected[0] - new Vector3((float)x1, (float)y1, 0)).Length() > (projected[1] - new Vector3((float)x1, (float)y1, 0)).Length())
            {
                var temp = tangents[0];
                tangents[0] = tangents[1];
                tangents[1] = temp;
            }
            if (xy1 == null || xy2 == null) return new SphereArc(intersection, tangents[0], tangents[1], true);
            return new SphereArc(intersection, xy1, xy2, true);
        }

        internal Circle3 GetUnitSphereVisibleCircle()
        {
            double cameraDistance = 9 * Math.Pow(0.5, cameraZoom) + 1;
            double tangentDistance = Math.Sqrt(cameraDistance * cameraDistance - 1);
            double angle = Math.Asin(1 / cameraDistance);
            double circleRadius = Math.Sin(angle) * tangentDistance;
            double centerDistance = Math.Sqrt(tangentDistance * tangentDistance - circleRadius * circleRadius);
            Vector3d cameraVector = To3D(new Vector3d(cameraRotX, cameraRotY, 0));
            return new Circle3(cameraVector * (cameraDistance - centerDistance), cameraVector, circleRadius);
        }

        internal bool IsUnitSpherePointVisible(Vector3d v)
        {
            double cameraDistance = 9 * Math.Pow(0.5, cameraZoom) + 1;
            double tangentDistance = Math.Sqrt(cameraDistance * cameraDistance - 1);
            return (v - GetPosition()).Length() <= tangentDistance;
        }

        // absolute coordinates
        private Vector3d GetPosition()
        {
            var cheat = new Vector3d(Unproject(new Vector3(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2, -10)));
            return cheat;
        }

        private double getFOV()
        {
            return Math.PI / 4 * Math.Pow(0.5, cameraZoom);
        }

        private Vector3d ToLatLong(Vector3d v)
        {
            return new Vector3d(Math.Atan2(v.X, -v.Y), Math.Asin(v.Z), 0);
        }

        public List<string> GetDebugInfo()
        {
            return new List<String> { "Controls: WASD, arrow keys, shift, space", $"{cameraRotX}:{cameraRotY}:{cameraZoom}" };
        }

        public List<IUIComponent> GetSettings()
        {
            return new List<IUIComponent>();
        }

        public List<IEditorGameComponent> GetSubComponents()
        {
            return new List<IEditorGameComponent>();
        }
    }
}

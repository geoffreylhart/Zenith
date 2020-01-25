using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.MathHelpers;
using Zenith.ZGame;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    public class EditorCamera : ZGameComponent
    {
        public double cameraRotX = 0; // longitude coordinate of our character
        public double cameraRotY = 0; // latitude coordinate of our character
        public double cameraZoom = 1; // no basis in reality (YET), positive means increased zoom, though
        public Matrix world;
        public Matrix view;
        public Matrix projection;

        public EditorCamera(Game game)
        {
        }

        public void UpdateCamera(GraphicsDevice graphicsDevice)
        {
            world = Matrix.CreateRotationZ(-(float)cameraRotX) * Matrix.CreateRotationX((float)cameraRotY); // eh.... think hard on this later
            float distance = (float)(9 * Math.Pow(0.5, cameraZoom));
            view = CameraMatrixManager.GetWorldView(distance);
            projection = CameraMatrixManager.GetWorldProjection(distance, graphicsDevice.Viewport.AspectRatio);
        }

        private float GetAspectRatio(GraphicsDevice graphicsDevice)
        {
            return graphicsDevice.Viewport.AspectRatio;
        }

        internal void ApplyMatrices(BasicEffect basicEffect)
        {
            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;
        }

        internal Vector3 Project(GraphicsDevice graphicsDevice, Vector3 v)
        {
            return graphicsDevice.Viewport.Project(v, projection, view, world);
        }

        internal Vector3 Unproject(GraphicsDevice graphicsDevice, Vector3 v)
        {
            return graphicsDevice.Viewport.Unproject(v, projection, view, world);
        }

        // yup, it returns lat/long only in the range you'd expect
        internal Vector3d GetLatLongOfCoord(GraphicsDevice graphicsDevice, double x, double y)
        {
            Matrixd worldd = Matrixd.CreateRotationZ(-cameraRotX) * Matrixd.CreateRotationX(cameraRotY);
            double distance = 9 * Math.Pow(0.5, cameraZoom);
            Matrixd viewd = CameraMatrixManager.GetWorldViewd(distance);
            Matrixd projectiond = CameraMatrixManager.GetWorldProjectiond(distance, graphicsDevice.Viewport.AspectRatio);

            Rayd ray = Rayd.CastFromCamera(graphicsDevice, x, y, projectiond, viewd, worldd);
            Vector3d intersection = ray.IntersectionSphere(new Vector3d(0, 0, 0), 1); // angle 0
            if (intersection == null) return null;
            return ToLatLong(intersection);
        }

        internal Rayd CastFromCamera(GraphicsDevice graphicsDevice, Vector2 mouseVector)
        {
            Matrixd worldd = Matrixd.CreateRotationZ(-cameraRotX) * Matrixd.CreateRotationX(cameraRotY);
            double distance = 9 * Math.Pow(0.5, cameraZoom);
            Matrixd viewd = CameraMatrixManager.GetWorldViewd(distance);
            Matrixd projectiond = CameraMatrixManager.GetWorldProjectiond(distance, graphicsDevice.Viewport.AspectRatio);
            return Rayd.CastFromCamera(graphicsDevice, mouseVector.X, mouseVector.Y, projectiond, viewd, worldd);
        }

        internal Vector3d GetUnitSphereIntersection(GraphicsDevice graphicsDevice, double x, double y)
        {
            var latlong = GetLatLongOfCoord(graphicsDevice, x, y);
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

        internal SphereArc GetArc(GraphicsDevice graphicsDevice, double x1, double y1, double x2, double y2)
        {
            Vector3 across = Unproject(graphicsDevice, new Vector3((float)x2, (float)y2, 1)) - Unproject(graphicsDevice, new Vector3((float)x1, (float)y1, 1));
            Vector3d into = CastFromCamera(graphicsDevice, new Vector2((float)x1, (float)y1)).Direction; // any vector along our slice that points away from the camera
            Vector3d normal = Vector3d.Cross(new Vector3d(across), into);
            Vector3d center = CastFromCamera(graphicsDevice, new Vector2((float)x1, (float)y1)).Position;
            var plane = new ZMath.Plane(center, normal);
            Circle3 intersection = plane.GetUnitSphereIntersection();
            if (intersection == null) return null;
            Vector3d[] tangents = intersection.GetTangents(GetPosition(graphicsDevice));
            Vector3[] projected = tangents.Select(x => Project(graphicsDevice, x.ToVector3())).ToArray();
            Vector3d xy1 = GetUnitSphereIntersection(graphicsDevice, x1, y1);
            Vector3d xy2 = GetUnitSphereIntersection(graphicsDevice, x2, y2);
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

        internal Circle3 GetUnitSphereVisibleCircle(GraphicsDevice graphicsDevice)
        {
            Vector3d position = GetPosition(graphicsDevice);
            double cameraDistance = position.Length();
            double tangentDistance = Math.Sqrt(cameraDistance * cameraDistance - 1);
            double angle = Math.Asin(1 / cameraDistance);
            double circleRadius = Math.Sin(angle) * tangentDistance;
            double centerDistance = Math.Sqrt(tangentDistance * tangentDistance - circleRadius * circleRadius);
            Vector3d cameraVector = position.Normalized();
            return new Circle3(cameraVector * (cameraDistance - centerDistance), cameraVector, circleRadius);
        }

        internal bool IsUnitSpherePointVisible(GraphicsDevice graphicsDevice, Vector3d v)
        {
            double cameraDistance = 9 * Math.Pow(0.5, cameraZoom) + 1;
            double tangentDistance = Math.Sqrt(cameraDistance * cameraDistance - 1);
            return (v - GetPosition(graphicsDevice)).Length() <= tangentDistance;
        }

        // absolute coordinates
        internal Vector3d GetPosition(GraphicsDevice graphicsDevice)
        {
            var cheat = new Vector3d(Unproject(graphicsDevice, new Vector3(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2, -10)));
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
    }
}

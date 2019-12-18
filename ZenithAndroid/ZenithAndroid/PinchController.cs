using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using Zenith;
using Zenith.EditorGameComponents;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace ZenithAndroid
{
    internal class PinchController : DrawableGameComponent
    {
        public Vector3d velocity = new Vector3d(0, 0, 0);
        private EditorCamera camera;

        public PinchController(Game game, EditorCamera camera) : base(game)
        {
            TouchPanel.EnabledGestures = GestureType.FreeDrag | GestureType.Pinch | GestureType.Tap;
            this.camera = camera;
            camera.cameraZoom = 2;
        }

        public override void Update(GameTime gameTime)
        {
            while (TouchPanel.IsGestureAvailable)
            {
                GestureSample gesture = TouchPanel.ReadGesture();

                if (gesture.GestureType == GestureType.FreeDrag)
                {
                    Vector2 b = gesture.Position; // current finger position
                    Vector2 a = b - gesture.Delta; // previous finger position

                    Vector3d a3 = camera.GetUnitSphereIntersection(a.X, a.Y);
                    Vector3d b3 = camera.GetUnitSphereIntersection(b.X, b.Y);
                    if (a3 != null && b3 != null)
                    {
                        UpdateCameraRotation(a3, b3);
                    }
                }
                else if (gesture.GestureType == GestureType.Pinch)
                {
                    Vector2 b = gesture.Position; // current finger position
                    Vector2 a = b - gesture.Delta; // previous finger position
                    Vector2 d = gesture.Position2; // current finger position
                    Vector2 c = d - gesture.Delta2; // previous finger position

                    Vector3d a3 = camera.GetUnitSphereIntersection(a.X, a.Y);
                    Vector3d b3 = camera.GetUnitSphereIntersection(b.X, b.Y);
                    Vector3d c3 = camera.GetUnitSphereIntersection(c.X, c.Y);
                    Vector3d d3 = camera.GetUnitSphereIntersection(d.X, d.Y);
                    if (a3 != null && b3 != null && c3 != null && d3 != null)
                    {
                        // this is likely not 100% accurate pinch, but good enough for awhile
                        Vector3d avg1 = camera.GetUnitSphereIntersection(a.X / 2 + c.X / 2, a.Y / 2 + c.Y / 2);
                        Vector3d avg2 = camera.GetUnitSphereIntersection(b.X / 2 + d.X / 2, b.Y / 2 + d.Y / 2);
                        if (avg1 != null && avg2 != null)
                        {
                            double cos1 = a3.Dot(c3);
                            double sin1 = a3.Cross(c3).Length();
                            double cos2 = b3.Dot(d3);
                            double sin2 = b3.Cross(d3).Length();
                            double angle1 = Math.Atan2(sin2, cos2);
                            double angle2 = Math.Atan2(sin1, cos1);
                            double scaleAmount;
                            if (angle1 < 2 * angle2 && angle1 > 0.5 * angle2) // let's ignore division by zero and extreme zoom for now
                            {
                                scaleAmount = angle1 / angle2;
                            }
                            else
                            {
                                scaleAmount = 1;
                            }
                            camera.cameraZoom += Math.Log(scaleAmount, 2);
                            UpdateCameraRotation(avg1, avg2);
                        }
                    }
                }
                else if (gesture.GestureType == GestureType.Tap)
                {
                    Vector2 a = gesture.Position; // current finger position

                    Vector3d a3 = camera.GetLatLongOfCoord(a.X, a.Y);
                    // center on the click and zoom in
                    if (a3 != null)
                    {
                        camera.cameraRotX = a3.X;
                        camera.cameraRotY = a3.Y;
                        camera.cameraZoom += 1;
                    }
                }
            }
            camera.UpdateCamera();
        }

        private void UpdateCameraRotation(Vector3d v1, Vector3d v2)
        {
            Vector3d center3 = To3D(new Vector3d(camera.cameraRotX, camera.cameraRotY, 0));
            center3 = ApplyRotation(center3, v1, v2);
            LongLat longLat = new SphereVector(center3).ToLongLat();
            camera.cameraRotX = longLat.X;
            camera.cameraRotY = longLat.Y;
        }

        // rotate v according to v1 -> v2
        private Vector3d ApplyRotation(Vector3d v, Vector3d v1, Vector3d v2)
        {
            Vector3d axis = v1.Cross(v2);
            if (axis.Length() > 0) // avoid division by zero
            {
                axis /= axis.Length();
            }
            else
            {
                return v;
            }
            double cosAngle = v1.Dot(v2);
            double sinAngle = v1.Cross(v2).Length();
            Vector3d posRot90 = v.Cross(axis);
            return v * cosAngle + posRot90 * sinAngle;
        }

        public override void Draw(GameTime gameTime)
        {
            int blah = 5;
        }

        internal static Vector3d To3D(Vector3d longLat)
        {
            double dz = Math.Sin(longLat.Y);
            double dxy = Math.Cos(longLat.Y); // the radius of the horizontal ring section, always positive
            double dx = Math.Sin(longLat.X) * dxy;
            double dy = -Math.Cos(longLat.X) * dxy;
            return new Vector3d(dx, dy, dz);
        }
    }
}

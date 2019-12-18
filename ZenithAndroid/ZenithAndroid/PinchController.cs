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
            TouchPanel.EnabledGestures = GestureType.FreeDrag | GestureType.DoubleTap | GestureType.Tap;
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
                        Vector3d center3 = To3D(new Vector3d(camera.cameraRotX, camera.cameraRotY, 0));
                        center3 = ApplyRotation(center3, a3, b3);
                        LongLat longLat = new SphereVector(center3).ToLongLat();
                        camera.cameraRotX = longLat.X;
                        camera.cameraRotY = longLat.Y;
                    }
                }
                else if (gesture.GestureType == GestureType.Tap)
                {
                    camera.cameraZoom += 1;
                }
                else if (gesture.GestureType == GestureType.DoubleTap)
                {
                    camera.cameraZoom += -2; // to compensate for the previous tap
                }
            }
            camera.UpdateCamera();
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

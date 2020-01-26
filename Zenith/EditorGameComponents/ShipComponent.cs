using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.ZGame;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    internal class ShipComponent : ZGameComponent
    {
        public Vector3d velocity = new Vector3d(0, 0, 0);
        public SphereVector forward = new SphereVector(0, 0, 1);
        public SphereVector position = new SphereVector(0, -1, 0);
        public double zoom = 1;
        private EditorCamera camera;
        double rotationSpeed = 0;

        public override void Update(GraphicsDevice graphicsDevice, GameTime gameTime)
        {
            if (!Game1.RECORDING) MoveShip(graphicsDevice);
        }

        private void MoveShip(GraphicsDevice graphicsDevice)
        {
            MoveShip1(graphicsDevice);
            //MoveShip2();
        }

        private void MoveShip1(GraphicsDevice graphicsDevice)
        {
            double rotationAccel = 0;
            double accel = AccelWithKeys();
            Keyboard.GetState().AffectNumber(ref rotationAccel, Keys.Left, Keys.Right, Keys.A, Keys.D, 0.01);
            rotationSpeed = Math.Max(Math.Min(rotationSpeed + rotationAccel, 0.1), -0.1);
            rotationSpeed *= 0.9;
            // apply rotation

            SphereVector right = new SphereVector(forward.Cross(position).Normalized());
            forward = forward.WalkTowards(right, rotationSpeed);

            // move forwards
            velocity += forward * accel * Math.Pow(0.5, zoom) * 20;
            position = new SphereVector((position + velocity).Normalized());
            // flatten our forward/velocity against current position
            forward = new SphereVector(forward.Cross(position).Cross(-position).Normalized());
            if (velocity.Length() > 0.0000000001)
            {
                double currentSpeed = velocity.Length();
                //if (rotationSpeed != 0) currentSpeed *= velocity.Normalized().Dot(forward);
                double max = Math.Pow(0.5, zoom) * 0.2;
                double min = Math.Pow(0.5, zoom) * 0;
                currentSpeed = Math.Max(Math.Min(currentSpeed, max), min);
                velocity = (velocity.Cross(position).Cross(-position).Normalized() * 0.8 + forward * 0.2).Normalized() * currentSpeed;
            }
            else
            {
                velocity = velocity.Cross(position).Cross(-position);
            }
            // now update altitude
            ControlZoomWithKeys();
            SphereVector unitPosition2 = new SphereVector(position.Normalized());
            camera.cameraRotX = unitPosition2.ToLongLat().X;
            camera.cameraRotY = unitPosition2.ToLongLat().Y;
            camera.UpdateCamera(graphicsDevice);
        }

        // have the mouse control everything
        private void MoveShip2(GraphicsDevice graphicsDevice)
        {
            double accel = AccelerateWithMouse(graphicsDevice);
            //double accel = AccelWithKeys();
            rotationSpeed = RotateWithMouse(graphicsDevice);
            SphereVector right = new SphereVector(forward.Cross(position).Normalized());
            forward = forward.WalkTowards(right, rotationSpeed);

            // move forwards
            velocity += forward * accel * Math.Pow(0.5, zoom) * 20;
            position = new SphereVector((position + velocity).Normalized());
            // flatten our forward/velocity against current position
            forward = new SphereVector(forward.Cross(position).Cross(-position).Normalized());
            if (velocity.Length() > 0.0000000001)
            {
                double currentSpeed = velocity.Length();
                //if (rotationSpeed != 0) currentSpeed *= velocity.Normalized().Dot(forward);
                double max = Math.Pow(0.5, zoom) * 0.2;
                double min = Math.Pow(0.5, zoom) * 0;
                currentSpeed = Math.Max(Math.Min(currentSpeed, max), min);
                velocity = (velocity.Cross(position).Cross(-position).Normalized() * 0.8 + forward * 0.2).Normalized() * currentSpeed;
            }
            else
            {
                velocity = velocity.Cross(position).Cross(-position);
            }
            // now update altitude
            SphereVector unitPosition2 = new SphereVector(position.Normalized());
            camera.cameraRotX = unitPosition2.ToLongLat().X;
            camera.cameraRotY = unitPosition2.ToLongLat().Y;
            BaseZoomOnSpeed();
            //ControlZoomWithKeys();
            camera.UpdateCamera(graphicsDevice);
        }

        public double RotateWithMouse(GraphicsDevice graphicsDevice)
        {
            SphereVector unitPosition = new SphereVector(position.Normalized());
            SphereVector up2 = unitPosition.WalkNorth(Math.PI / 2);
            SphereVector right2 = new SphereVector(up2.Cross(unitPosition).Normalized());
            double screenSpaceRotation = Math.Atan2(-forward.Dot(up2), forward.Dot(right2)); // we want up to be 0 and a positive rotation to be cw

            Point mousePos = Mouse.GetState().Position;
            Vector2d mouseV = new Vector2d((mousePos.X / (double)graphicsDevice.Viewport.Width - 0.5) * graphicsDevice.Viewport.AspectRatio, mousePos.Y / (double)graphicsDevice.Viewport.Height - 0.5);
            double mouseRotation = Math.Atan2(mouseV.Y, mouseV.X);
            double diff1 = (mouseRotation + 4 * Math.PI - screenSpaceRotation) % (2 * Math.PI);
            double diff2 = (screenSpaceRotation + 4 * Math.PI - mouseRotation) % (2 * Math.PI);
            double rotationSpeed;
            if (diff1 < diff2)
            {
                return diff1 / 10;
            }
            else
            {
                return -diff2 / 10;
            }
        }

        public double AccelWithKeys()
        {
            double accel = 0;
            Keyboard.GetState().AffectNumber(ref accel, Keys.Down, Keys.Up, Keys.S, Keys.W, 0.002);
            return accel;
        }

        public double AccelerateWithMouse(GraphicsDevice graphicsDevice)
        {
            Point mousePos = Mouse.GetState().Position;
            Vector2d mouseV = new Vector2d((mousePos.X / (double)graphicsDevice.Viewport.Width - 0.5) * graphicsDevice.Viewport.AspectRatio, mousePos.Y / (double)graphicsDevice.Viewport.Height - 0.5);
            return (mouseV.Length() - 0.25) / 1000;
        }

        public void BaseZoomOnSpeed()
        {
            zoom = Math.Min(20, -3 + Math.Log(velocity.Length()) / Math.Log(0.5));
            camera.cameraZoom = zoom;
        }

        private void ControlZoomWithKeys()
        {
            if (Keyboard.GetState().WasKeyPressed(Keys.LeftShift)) zoom += 3;
            if (Keyboard.GetState().WasKeyPressed(Keys.Space)) zoom -= 3;
            camera.cameraZoom = zoom * 0.05 + camera.cameraZoom * 0.95;
        }


        public ShipComponent(Game game, EditorCamera camera)
        {
            this.camera = camera;
        }

        public override void Draw(RenderContext renderContext, GameTime gameTime)
        {
            if (Game1.RECORDING) MoveShip(renderContext.graphicsDevice);
            SphereVector unitPosition = new SphereVector(position.Normalized());
            SphereVector up = unitPosition.WalkNorth(Math.PI / 2);
            SphereVector right = new SphereVector(up.Cross(unitPosition).Normalized());
            double rotation = Math.Atan2(forward.Dot(-right), forward.Dot(up)); // we want up to be 0 and a positive rotation to be cw
            Matrixd shipWVP = CreateShipWVP(renderContext, rotation);
            foreach (ModelMesh mesh in GlobalContent.StartingShuttle.Meshes)
            {
                foreach (BasicEffect eff in mesh.Effects)
                {
                    eff.EnableDefaultLighting();
                    eff.World = shipWVP.toMatrix();
                    eff.VertexColorEnabled = false;
                    eff.Alpha = 1;
                }

                mesh.Draw();
            }
        }

        private Matrixd CreateShipWVP(RenderContext renderContext, double rotation)
        {
            double fakeScale = Math.Max(Math.Pow(2, 21 - camera.cameraZoom), 1); // make the ship bigger to account for depth buffer limitations
            Vector3d shipPos = position;
            // earth radius 6371 km
            double shipScale = fakeScale / 6371000;
            double distanceFromCamera = 30; // in meters
            Matrixd world = Matrixd.CreateRotationZ(-camera.cameraRotX) * Matrixd.CreateRotationX(camera.cameraRotY); // eh.... think hard on this later
            double distance = 9 * Math.Pow(0.5, camera.cameraZoom);
            shipPos += position.WalkNorth(-Math.PI / 4) * (distance * 2 - distanceFromCamera * shipScale); // why distance * 2??
            Matrixd view = CameraMatrixManager.GetWorldViewd(distance);
            Matrixd projection = CameraMatrixManager.GetWorldProjectiond(distance, renderContext.graphicsDevice.Viewport.AspectRatio);
            Matrixd worldWVP = world * view * projection;
            Matrixd shipRot = Matrixd.CreateRotationZ(rotation + Math.PI) * Matrixd.CreateRotationX(Math.PI / 2 - camera.cameraRotY) * Matrixd.CreateRotationZ(camera.cameraRotX);
            return shipRot * Matrixd.CreateScale(shipScale) * Matrixd.CreateTranslation(shipPos) * worldWVP;
        }
    }
}

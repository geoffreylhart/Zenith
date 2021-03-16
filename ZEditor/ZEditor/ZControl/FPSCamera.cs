using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    class FPSCamera
    {
        private Vector3 cameraPosition;
        private Vector3 cameraLookUnitVector;

        public FPSCamera(Vector3 cameraPosition, Vector3 cameraTarget)
        {
            this.cameraPosition = cameraPosition;
            cameraLookUnitVector = cameraTarget - cameraPosition;
            cameraLookUnitVector.Normalize();
        }

        internal Matrix GetView()
        {
            return Matrix.CreateLookAt(cameraPosition, cameraPosition + cameraLookUnitVector, Vector3.Up);
        }

        private KeyboardState? prevKeyboardState = null;
        private MouseState? prevMouseState = null;
        internal void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, GraphicsDevice graphicsDevice)
        {
            // update mouse look vector, for now, let's assume that we'll want to track the mouse perfectly
            float relx = graphicsDevice.Viewport.Width / 2f;
            float rely = graphicsDevice.Viewport.Height / 2f;

            float diffx = 0;
            float diffy = 0;
            if (prevMouseState.HasValue && gameTime.TotalGameTime.TotalSeconds > 0.1)
            {
                diffx = mouseState.X - relx;
                diffy = mouseState.Y - rely;
            }
            relx += diffx * 3;
            rely += diffy * 3;
            Matrix world = Matrix.Identity;
            Matrix view = GetView();
            Matrix projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), graphicsDevice.Viewport.AspectRatio, 0.01f, 10f);
            Vector3 unprojected = graphicsDevice.Viewport.Unproject(new Vector3(relx, rely, 0.25f), projection, view, world);
            Vector3 unprojected2 = graphicsDevice.Viewport.Unproject(new Vector3(relx, rely, 0.75f), projection, view, world);
            var blah = graphicsDevice.Viewport.MinDepth;
            var newCameraLookUnitVector = unprojected2 - unprojected;
            newCameraLookUnitVector.Normalize();
            cameraLookUnitVector = newCameraLookUnitVector;

            //float diffx = 0;
            //float diffy = 0;
            //if (prevMouseState.HasValue)
            //{
            //    diffx = mouseState.X - prevMouseState.Value.X;
            //    diffy = mouseState.Y - prevMouseState.Value.Y;
            //}
            //cameraLookUnitVector = Vector3.Transform(cameraLookUnitVector, Matrix.CreateRotationY(diffx / -400f));
            //cameraLookUnitVector.Normalize();
            //cameraLookUnitVector.Y += diffy / -200f;
            //cameraLookUnitVector.Normalize();

            float walkSpeed = 4.317f;
            float ascendSpeed = walkSpeed; // not sure
            // minecraft walk speed is about 4.317 m/s, sprint is 5.612, jump-sprint is 7.143, fly is 10.92, fly-sprint is 21.6
            Vector3 flatCameraLookUnitVector = new Vector3(cameraLookUnitVector.X, 0, cameraLookUnitVector.Z);
            flatCameraLookUnitVector.Normalize();
            // TODO: I guess this is counter-clockwise around y-axis??
            Vector3 flatRightUnitVector = Vector3.Transform(flatCameraLookUnitVector, Matrix.CreateRotationY((float)(-Math.PI / 2)));
            float forwardAmount = 0;
            float rightAmount = 0;
            float upAmount = 0;
            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up)) forwardAmount++;
            if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down)) forwardAmount--;
            if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right)) rightAmount++;
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left)) rightAmount--;
            if (keyboardState.IsKeyDown(Keys.Space)) upAmount++;
            if (keyboardState.IsKeyDown(Keys.LeftShift)) upAmount--;
            float len = (float)Math.Sqrt(forwardAmount * forwardAmount + rightAmount * rightAmount);
            if (len > 0)
            {
                forwardAmount /= len;
                rightAmount /= len;
            }
            cameraPosition += flatCameraLookUnitVector * forwardAmount * (float)gameTime.ElapsedGameTime.TotalSeconds * walkSpeed;
            cameraPosition += flatRightUnitVector * rightAmount * (float)gameTime.ElapsedGameTime.TotalSeconds * walkSpeed;
            cameraPosition += Vector3.Up * upAmount * (float)gameTime.ElapsedGameTime.TotalSeconds * ascendSpeed;

            prevKeyboardState = keyboardState;
            prevMouseState = mouseState;
        }
    }
}

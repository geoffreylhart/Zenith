using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    public abstract class AbstractCamera
    {
        protected Vector3 cameraPosition;
        protected Vector3 cameraLookUnitVector;
        protected Vector3 cameraUpVector;

        public AbstractCamera(Vector3 cameraPosition, Vector3 cameraTarget)
        {
            this.cameraPosition = cameraPosition;
            cameraLookUnitVector = cameraTarget - cameraPosition;
            cameraLookUnitVector.Normalize();
            cameraUpVector = Vector3.Up;
        }

        public Vector3 GetPosition()
        {
            return cameraPosition;
        }

        public Matrix GetView()
        {
            return Matrix.CreateLookAt(cameraPosition, cameraPosition + cameraLookUnitVector, cameraUpVector);
        }

        public abstract void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, GraphicsDevice graphicsDevice);

        public Vector3 GetLookUnitVector(float mouseX, float mouseY, GraphicsDevice graphicsDevice)
        {
            Matrix world = Matrix.Identity;
            Matrix view = GetView();
            Matrix projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), graphicsDevice.Viewport.AspectRatio, 0.01f, 10f);
            Vector3 unprojected = graphicsDevice.Viewport.Unproject(new Vector3(mouseX, mouseY, 0.25f), projection, view, world);
            Vector3 unprojected2 = graphicsDevice.Viewport.Unproject(new Vector3(mouseX, mouseY, 0.75f), projection, view, world);
            var newCameraLookUnitVector = unprojected2 - unprojected;
            newCameraLookUnitVector.Normalize();
            return newCameraLookUnitVector;
        }

        public Vector3 GetRelativeVector(Vector3 v)
        {
            Vector3 forward = cameraLookUnitVector;
            Vector3 right = Vector3.Cross(forward, cameraUpVector);
            Vector3 up = Vector3.Cross(right, forward);
            return v.X * right + v.Y * up - v.Z * forward;
        }

        internal Vector3 GetTarget()
        {
            return cameraPosition + cameraLookUnitVector;
        }
    }
}

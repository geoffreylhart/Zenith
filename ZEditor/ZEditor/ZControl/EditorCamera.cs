using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    class EditorCamera : AbstractCamera
    {
        private KeyboardState? prevKeyboardState = null;
        private MouseState? prevMouseState = null;
        private Point? prevMousePosition = null;

        public EditorCamera(Vector3 cameraPosition, Vector3 cameraTarget) : base(cameraPosition, Vector3.Zero)
        {
        }

        public override void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, GraphicsDevice graphicsDevice)
        {
            if (prevMouseState.HasValue)
            {
                cameraPosition *= (float)Math.Pow(0.999, mouseState.ScrollWheelValue - prevMouseState.Value.ScrollWheelValue);
            }
            bool dragMode = prevMouseState.HasValue && prevMouseState.Value.MiddleButton == ButtonState.Pressed && mouseState.MiddleButton == ButtonState.Pressed;
            if (dragMode)
            {
                float diffx = mouseState.X - prevMousePosition.Value.X;
                float diffy = mouseState.Y - prevMousePosition.Value.Y;
                float distance = cameraPosition.Length();
                Vector3 rightVector = GetRelativeVector(Vector3.Right);
                Vector3 upVector = GetRelativeVector(Vector3.Up);
                upVector.Normalize();
                cameraPosition += diffx * distance / 100 * rightVector - diffy * distance / 100 * upVector;
                cameraPosition = cameraPosition / cameraPosition.Length() * distance;
                cameraLookUnitVector = -cameraPosition;
                cameraLookUnitVector.Normalize();
                Vector3 newRightVector = GetRelativeVector(Vector3.Right);
                if (Vector3.Dot(rightVector, newRightVector) < 0)
                {
                    cameraUpVector = -cameraUpVector;
                }
            }
            prevKeyboardState = keyboardState;
            prevMouseState = mouseState;
            prevMousePosition = mouseState.Position;
            if (dragMode)
            {
                if (mouseState.X < 0 || mouseState.Y < 0 || mouseState.X > graphicsDevice.Viewport.Width || mouseState.Y > graphicsDevice.Viewport.Height)
                {
                    Vector2 offsetAmount = new Vector2((mouseState.X + graphicsDevice.Viewport.Width) % graphicsDevice.Viewport.Width - mouseState.X, (mouseState.Y + graphicsDevice.Viewport.Height) % graphicsDevice.Viewport.Height - mouseState.Y);
                    Mouse.SetPosition(mouseState.X + (int)offsetAmount.X, mouseState.Y + (int)offsetAmount.Y);
                    prevMousePosition = new Point(prevMousePosition.Value.X + (int)offsetAmount.X, prevMousePosition.Value.Y + (int)offsetAmount.Y);
                }
            }
        }
    }
}

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
        public EditorCamera(Vector3 cameraPosition, Vector3 cameraTarget) : base(cameraPosition, Vector3.Zero)
        {
        }

        public override void Update(UIContext uiContext)
        {
            cameraPosition *= (float)Math.Pow(0.999, uiContext.ScrollWheelDiff);
            bool dragMode = Mouse.GetState().MiddleButton == ButtonState.Pressed;
            if (dragMode)
            {
                Vector2 diff = uiContext.MouseDiffVector2;
                float distance = cameraPosition.Length();
                Vector3 rightVector = GetRelativeVector(Vector3.Right);
                Vector3 upVector = GetRelativeVector(Vector3.Up);
                upVector.Normalize();
                cameraPosition += diff.X * distance / 100 * rightVector - diff.Y * distance / 100 * upVector;
                cameraPosition = cameraPosition / cameraPosition.Length() * distance;
                cameraLookUnitVector = -cameraPosition;
                cameraLookUnitVector.Normalize();
                Vector3 newRightVector = GetRelativeVector(Vector3.Right);
                if (Vector3.Dot(rightVector, newRightVector) < 0)
                {
                    cameraUpVector = -cameraUpVector;
                }
            }
            if (dragMode) uiContext.WrapAroundMouse();
        }
    }
}

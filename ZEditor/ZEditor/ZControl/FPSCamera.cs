using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    public class FPSCamera : AbstractCamera
    {
        public FPSCamera(Vector3 cameraPosition, Vector3 cameraTarget) : base(cameraPosition, cameraTarget)
        {
        }

        public override void Update(UIContext uiContext)
        {
            uiContext.CenterMouse();
            // update mouse look vector, for now, let's assume that we'll want to track the mouse perfectly
            Vector2 relative = uiContext.MouseVector2 + uiContext.MouseDiffVector2 * 3;
            Matrix world = Matrix.Identity;
            Matrix view = GetView();
            Matrix projection = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 4), uiContext.AspectRatio, 0.01f, 10f);
            Vector3 unprojected = uiContext.Unproject(new Vector3(relative.X, relative.Y, 0.25f), projection, view, world);
            Vector3 unprojected2 = uiContext.Unproject(new Vector3(relative.X, relative.Y, 0.75f), projection, view, world);
            var newCameraLookUnitVector = unprojected2 - unprojected;
            newCameraLookUnitVector.Normalize();
            cameraLookUnitVector = newCameraLookUnitVector;

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
            if (Keyboard.GetState().IsKeyDown(Keys.W) || Keyboard.GetState().IsKeyDown(Keys.Up)) forwardAmount++;
            if (Keyboard.GetState().IsKeyDown(Keys.S) || Keyboard.GetState().IsKeyDown(Keys.Down)) forwardAmount--;
            if (Keyboard.GetState().IsKeyDown(Keys.D) || Keyboard.GetState().IsKeyDown(Keys.Right)) rightAmount++;
            if (Keyboard.GetState().IsKeyDown(Keys.A) || Keyboard.GetState().IsKeyDown(Keys.Left)) rightAmount--;
            if (Keyboard.GetState().IsKeyDown(Keys.Space)) upAmount++;
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift)) upAmount--;
            float len = (float)Math.Sqrt(forwardAmount * forwardAmount + rightAmount * rightAmount);
            if (len > 0)
            {
                forwardAmount /= len;
                rightAmount /= len;
            }
            cameraPosition += flatCameraLookUnitVector * forwardAmount * (float)uiContext.ElapsedSeconds * walkSpeed;
            cameraPosition += flatRightUnitVector * rightAmount * (float)uiContext.ElapsedSeconds * walkSpeed;
            cameraPosition += Vector3.Up * upAmount * (float)uiContext.ElapsedSeconds * ascendSpeed;
        }
    }
}

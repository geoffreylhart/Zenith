using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    public interface IUIContext
    {

        public AbstractCamera Camera { get; set; }

        public float AspectRatio { get; }

        public double ScrollWheelDiff { get; }

        public void CenterMouse();
        public Vector2 MouseVector2 { get; }

        public double ElapsedSeconds { get; }

        public Vector2 MouseDiffVector2 { get; }

        public Vector2 ScreenCenter { get; }

        public bool IsKeyPressed(Keys key);

        public bool IsShiftPressed();

        public bool IsCtrlPressed();

        public bool IsKeyShiftPressed(Keys key);

        public bool IsKeyCtrlPressed(Keys key);

        public void WrapAroundMouse();

        public Vector3 Project(Vector3 v, Matrix projection, Matrix view, Matrix world);

        public Vector3 Unproject(Vector3 v, Matrix projection, Matrix view, Matrix world);

        public void UpdateKeys();

        public void UpdateGameTime(GameTime gameTime);

        public bool IsLeftMouseButtonPressed();

        public bool IsRightMouseButtonPressed();
    }
}

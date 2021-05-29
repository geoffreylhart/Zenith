using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;

namespace ZEditorUnitTests.UITests
{
    public class MockUIContext : IUIContext
    {
        // control commands
        Keys? keysPressed = null;
        public void SetKeyPressed(Keys? keys)
        {
            keysPressed = keys;
        }
        // implementation
        public AbstractCamera Camera { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public float AspectRatio => throw new NotImplementedException();

        public double ScrollWheelDiff => throw new NotImplementedException();

        public Vector2 MouseVector2 => throw new NotImplementedException();

        public double ElapsedSeconds => throw new NotImplementedException();

        public Vector2 MouseDiffVector2 => throw new NotImplementedException();

        public Vector2 ScreenCenter => throw new NotImplementedException();

        public void CenterMouse()
        {
            throw new NotImplementedException();
        }

        public bool IsCtrlPressed()
        {
            return false;
        }

        public bool IsKeyCtrlPressed(Keys key)
        {
            return false;
        }

        public bool IsKeyPressed(Keys key)
        {
            return key == keysPressed;
        }

        public bool IsKeyShiftPressed(Keys key)
        {
            return false;
        }

        public bool IsLeftMouseButtonPressed()
        {
            return false;
        }

        public bool IsRightMouseButtonPressed()
        {
            return false;
        }

        public bool IsShiftPressed()
        {
            return false;
        }

        public Vector3 Project(Vector3 v, Matrix projection, Matrix view, Matrix world)
        {
            throw new NotImplementedException();
        }

        public Vector3 Unproject(Vector3 v, Matrix projection, Matrix view, Matrix world)
        {
            throw new NotImplementedException();
        }

        public void UpdateGameTime(GameTime gameTime)
        {
            throw new NotImplementedException();
        }

        public void UpdateKeys()
        {
            throw new NotImplementedException();
        }

        public void WrapAroundMouse()
        {
            throw new NotImplementedException();
        }
    }
}

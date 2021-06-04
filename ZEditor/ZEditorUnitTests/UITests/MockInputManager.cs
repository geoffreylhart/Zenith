using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;

namespace ZEditorUnitTests.UITests
{
    public class MockInputManager : IInputManager
    {
        Keys? keysPressed = null;

        public KeyboardState GetKeyboardState()
        {
            if (keysPressed.HasValue)
            {
                return new KeyboardState(keysPressed.Value);
            }
            else
            {
                return new KeyboardState();
            }
        }

        public MouseState GetMouseState()
        {
            return new MouseState();
        }

        internal void SetKeysDown(Keys? keys)
        {
            keysPressed = keys;
        }
    }
}

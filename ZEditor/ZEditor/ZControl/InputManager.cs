using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    public class InputManager : IInputManager
    {
        public KeyboardState GetKeyboardState()
        {
            return Keyboard.GetState();
        }

        public MouseState GetMouseState()
        {
            return Mouse.GetState();
        }
    }
}

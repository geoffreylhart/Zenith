using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    public interface IInputManager
    {
        KeyboardState GetKeyboardState();
        MouseState GetMouseState();
    }
}

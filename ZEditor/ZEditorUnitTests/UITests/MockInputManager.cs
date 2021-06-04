using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;

namespace ZEditorUnitTests.UITests
{
    public class MockInputManager : IInputManager
    {
        public KeyboardState GetKeyboardState()
        {
            throw new NotImplementedException();
        }

        public MouseState GetMouseState()
        {
            throw new NotImplementedException();
        }
    }
}

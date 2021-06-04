using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZManage;

namespace ZEditor.ZControl
{
    public class InputListener
    {
        private Trigger trigger;
        private Action<InputListener> action;
        public InputListener(Trigger trigger, Action<InputListener> action)
        {
            this.trigger = trigger;
            this.action = action;
        }

        public void Trigger()
        {
            action(this);
        }

        public bool CheckMainDown(MouseState mouseState, KeyboardState keyboardState)
        {
            bool shiftIsDown = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            bool ctrlIsDown = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            if (trigger.isMiddleMouse) return mouseState.MiddleButton == ButtonState.Pressed;
            if (trigger.isRightMouse) return mouseState.RightButton == ButtonState.Pressed;
            if (trigger.isLeftMouse) return mouseState.LeftButton == ButtonState.Pressed;
            if (trigger.key.HasValue) return keyboardState.IsKeyDown(trigger.key.Value);
            if (trigger.isShift) return shiftIsDown;
            if (trigger.isCtrl) return ctrlIsDown;
            return false;
        }

        public bool CheckAllDown(MouseState mouseState, KeyboardState keyboardState)
        {
            bool shiftIsDown = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            bool ctrlIsDown = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            bool isDown = trigger.key.HasValue ? keyboardState.IsKeyDown(trigger.key.Value) : true;
            isDown &= !trigger.isShift || shiftIsDown;
            isDown &= !trigger.isCtrl || ctrlIsDown;
            isDown &= !trigger.isLeftMouse || mouseState.LeftButton == ButtonState.Pressed;
            isDown &= !trigger.isMiddleMouse || mouseState.MiddleButton == ButtonState.Pressed;
            isDown &= !trigger.isRightMouse || mouseState.RightButton == ButtonState.Pressed;
            isDown &= !(trigger.isExclusive && !trigger.isShift && shiftIsDown);
            isDown &= !(trigger.isExclusive && !trigger.isCtrl && ctrlIsDown);
            return isDown;
        }
    }
}

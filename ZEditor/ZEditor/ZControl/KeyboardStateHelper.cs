using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    public static class KeyboardStateHelper
    {
        static Dictionary<Keys, bool> prevCtrlStates = new Dictionary<Keys, bool>();
        public static bool AreKeysCtrlPressed(this KeyboardState state, Keys key)
        {
            if (!prevCtrlStates.ContainsKey(key)) prevCtrlStates[key] = false;
            bool answer = state.IsKeyDown(key) && !prevCtrlStates[key];
            prevCtrlStates[key] = state.IsKeyDown(key);
            return answer;
        }
    }
}

using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.UI
{
    public class StateSwitcher : ZComponent
    {
        private ZComponent defaultState;
        private Dictionary<Keys, ZComponent> keyStates = new Dictionary<Keys, ZComponent>();
        private Dictionary<Keys, Action> keyActions = new Dictionary<Keys, Action>();
        private ZComponent currentState;

        public StateSwitcher(ZComponent defaultState)
        {
            this.defaultState = defaultState;
            this.currentState = defaultState;
        }

        // TODO: maybe use endless interfaces so we can request something that is actually a ui thing, eh?
        internal void AddKeyState(Keys key, ZComponent state, Action onSwitchAction)
        {
            keyStates.Add(key, state);
            keyActions.Add(key, onSwitchAction);
        }

        public override void Update(UIContext uiContext)
        {
            foreach (var key in keyStates.Keys)
            {
                if (uiContext.IsKeyPressed(key))
                {
                    currentState = keyStates[key];
                    keyActions[key]();
                }
            }
            if (uiContext.IsKeyPressed(Keys.Escape))
            {
                currentState = defaultState;
            }
            currentState.Update(uiContext);
        }
    }
}

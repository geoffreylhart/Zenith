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
        private List<State> states = new List<State>();
        private ZComponent currentState;

        public StateSwitcher(ZComponent defaultState)
        {
            this.defaultState = defaultState;
            this.currentState = defaultState;
        }

        // TODO: maybe use endless interfaces so we can request something that is actually a ui thing, eh?
        public void AddKeyState(Keys key, ZComponent state, Action onSwitchAction)
        {
            states.Add(new State(x => x.IsKeyPressed(key), state, onSwitchAction));
        }

        public void AddShiftKeyState(Keys key, ZComponent state, Action onSwitchAction)
        {
            states.Add(new State(x => x.IsKeyShiftPressed(key), state, onSwitchAction));
        }

        public override void Update(IUIContext uiContext)
        {
            foreach (var state in states)
            {
                if (state.triggered(uiContext))
                {
                    if (currentState == state.state)
                    {
                        currentState = defaultState;
                    }
                    else
                    {
                        currentState = state.state;
                        state.onSwitchAction();
                    }
                }
            }
            if (uiContext.IsKeyPressed(Keys.Escape) || uiContext.IsLeftMouseButtonPressed())
            {
                currentState = defaultState;
            }
            currentState.Update(uiContext);
        }

        private class State
        {
            public Func<IUIContext, bool> triggered;
            public ZComponent state;
            public Action onSwitchAction;

            public State(Func<IUIContext, bool> triggered, ZComponent state, Action onSwitchAction)
            {
                this.triggered = triggered;
                this.state = state;
                this.onSwitchAction = onSwitchAction;
            }
        }
    }
}

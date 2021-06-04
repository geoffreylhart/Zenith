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
        public void AddKeyState(Trigger key, ZComponent state, Action onSwitchAction)
        {
            states.Add(new State(key, state, onSwitchAction));
            RegisterListener(new InputListener(key, x =>
            {
                onSwitchAction();
                currentState = state;
                state.Focus();
                var escapeListeners = new List<InputListener>();
                foreach(var trigger in new[] { Trigger.Escape, Trigger.LeftMouseClick, key })
                {
                    var listener = new InputListener(trigger, y =>
                    {
                        this.Focus();
                        currentState = defaultState;
                        foreach (var l in escapeListeners) currentState.UnregisterListener(l);
                    });
                    escapeListeners.Add(listener);
                }
            }));
        }

        private class State
        {
            public Trigger keyMouseCombo;
            public ZComponent state;
            public Action onSwitchAction;

            public State(Trigger keyCombo, ZComponent state, Action onSwitchAction)
            {
                this.keyMouseCombo = keyCombo;
                this.state = state;
                this.onSwitchAction = onSwitchAction;
            }
        }
    }
}

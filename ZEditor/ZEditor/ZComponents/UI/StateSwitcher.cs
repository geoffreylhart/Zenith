using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.UI
{
    // TODO: ugh... our current setup of only one focused item doesnt work
    // I think we'll need arbitrary listen/stoplistening functions
    public class StateSwitcher : ZGameObject
    {
        private ZComponent defaultState;
        private ZComponent currentState;

        public StateSwitcher(ZComponent defaultState)
        {
            this.defaultState = defaultState;
            this.currentState = defaultState;
            Register(currentState);
        }

        // TODO: maybe use endless interfaces so we can request something that is actually a ui thing, eh?
        public void AddKeyState(Trigger key, ZComponent state, Action onSwitchAction, bool pressAgainToRevert)
        {
            RegisterListener(new InputListener(key, x =>
            {
                var oldFocus = state.GetFocus();
                onSwitchAction();
                currentState = state;
                state.Focus();
                var escapeListeners = new List<InputListener>();
                var escapeTriggers = new List<Trigger>() { Trigger.Escape, Trigger.LeftMouseClick };
                if (pressAgainToRevert) escapeTriggers.Add(key);
                foreach (var trigger in escapeTriggers)
                {
                    var listener = new InputListener(trigger, y =>
                    {
                        oldFocus.Focus();
                        currentState = defaultState;
                        foreach (var l in escapeListeners) state.UnregisterListener(l);
                    });
                    state.RegisterListener(listener);
                    escapeListeners.Add(listener);
                }
            }));
        }

        public override void Update(UIContext uiContext)
        {
            currentState.Update(uiContext);
        }
    }
}

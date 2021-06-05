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
        private ZComponent defaultFocus;
        private ZComponent currentFocus;

        public StateSwitcher()
        {
            this.defaultFocus = this;
            this.currentFocus = this;
        }

        public StateSwitcher(ZComponent defaultFocus)
        {
            this.defaultFocus = defaultFocus;
            this.currentFocus = defaultFocus;
        }

        // TODO: maybe use endless interfaces so we can request something that is actually a ui thing, eh?
        public void AddKeyFocus(Trigger key, ZComponent focus, Action onSwitchAction, bool pressAgainToRevert)
        {
            RegisterListener(new InputListener(key, x =>
            {
                onSwitchAction();
                currentFocus = focus;
                focus.Focus();
                var escapeListeners = new List<InputListener>();
                var escapeTriggers = new List<Trigger>() { Trigger.Escape, Trigger.LeftMouseClick };
                if (pressAgainToRevert) escapeTriggers.Add(key);
                foreach (var trigger in escapeTriggers)
                {
                    var listener = new InputListener(trigger, y =>
                    {
                        defaultFocus.Focus();
                        currentFocus = defaultFocus;
                        foreach (var l in escapeListeners) focus.UnregisterListener(l);
                    });
                    focus.RegisterListener(listener);
                    escapeListeners.Add(listener);
                }
            }));
        }

        public override void Update(UIContext uiContext)
        {
            // TODO: make this feel more natural...
            if (currentFocus != defaultFocus)
            {
                currentFocus.Update(uiContext);
            }
        }
    }
}

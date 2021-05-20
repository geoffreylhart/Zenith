using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.UI
{
    public class ActionTriggerer : ZComponent
    {
        private List<Trigger> triggers = new List<Trigger>();

        // TODO: maybe use endless interfaces so we can request something that is actually a ui thing, eh?
        public void AddKeyTrigger(Keys key, Action onSwitchAction)
        {
            triggers.Add(new Trigger(x => x.IsKeyPressed(key), onSwitchAction));
        }

        public void AddShiftKeyTrigger(Keys key, Action onSwitchAction)
        {
            triggers.Add(new Trigger(x => x.IsKeyShiftPressed(key), onSwitchAction));
        }

        public override void Update(UIContext uiContext)
        {
            foreach (var state in triggers)
            {
                if (state.triggered(uiContext))
                {
                    state.onSwitchAction();
                }
            }
        }

        private class Trigger
        {
            public Func<UIContext, bool> triggered;
            public Action onSwitchAction;

            public Trigger(Func<UIContext, bool> triggered, Action onSwitchAction)
            {
                this.triggered = triggered;
                this.onSwitchAction = onSwitchAction;
            }
        }
    }
}

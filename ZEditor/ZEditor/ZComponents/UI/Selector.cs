using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.UI
{
    // can select multiple items
    // TODO: perhaps separate components that merely facilitate actions vs ones that are visible
    public class Selector : ZComponent
    {
        public HashSet<int> selected = new HashSet<int>();
        private IIndexSelectionProvider indexSelectionProvider;
        private Action<int> OnSelect;
        private Action<int> OnDeselect;

        public Selector(IIndexSelectionProvider indexSelectionProvider, Action<int> OnSelect, Action<int> OnDeselect)
        {
            this.indexSelectionProvider = indexSelectionProvider;
            this.OnSelect = OnSelect;
            this.OnDeselect = OnDeselect;
        }

        public override void Update(UIContext uiContext)
        {
            if (uiContext.IsLeftMouseButtonPressed())
            {
                int selectedIndex = indexSelectionProvider.GetSelectedIndex(uiContext);
                if (uiContext.IsCtrlPressed())
                {

                }
                else if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift))
                {
                    if (selected.Contains(selectedIndex))
                    {
                        OnDeselect(selectedIndex);
                        selected.Remove(selectedIndex);
                    }
                    else
                    {
                        OnSelect(selectedIndex);
                        selected.Add(selectedIndex);
                    }
                }
                else
                {
                    foreach (var v in selected)
                    {
                        if (v != selectedIndex)
                        {
                            OnDeselect(v);
                        }
                    }
                    if (!selected.Contains(selectedIndex))
                    {
                        OnSelect(selectedIndex);
                    }
                    selected.Clear();
                    selected.Add(selectedIndex);
                }
            }
        }
    }
}

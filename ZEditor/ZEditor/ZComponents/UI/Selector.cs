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
    public class Selector<T> : ZComponent
    {
        public HashSet<T> selected = new HashSet<T>(); // TODO: make this readonly?
        private IIndexSelectionProvider<T> indexSelectionProvider;
        private Action<T> OnSelect;
        private Action<T> OnDeselect;

        public Selector(IIndexSelectionProvider<T> indexSelectionProvider, Action<T> OnSelect, Action<T> OnDeselect)
        {
            this.indexSelectionProvider = indexSelectionProvider;
            this.OnSelect = OnSelect;
            this.OnDeselect = OnDeselect;
        }

        public override void Update(IUIContext uiContext)
        {
            if (uiContext.IsLeftMouseButtonPressed())
            {
                T selected = indexSelectionProvider.GetSelectedIndex(uiContext);
                if (uiContext.IsCtrlPressed())
                {

                }
                else if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift))
                {
                    if (this.selected.Contains(selected))
                    {
                        OnDeselect(selected);
                        this.selected.Remove(selected);
                    }
                    else
                    {
                        OnSelect(selected);
                        this.selected.Add(selected);
                    }
                }
                else
                {
                    foreach (var v in this.selected)
                    {
                        if (!v.Equals(selected))
                        {
                            OnDeselect(v);
                        }
                    }
                    if (!this.selected.Contains(selected))
                    {
                        OnSelect(selected);
                    }
                    this.selected.Clear();
                    this.selected.Add(selected);
                }
            }
        }

        internal void Clear()
        {
            foreach (var v in selected)
            {
                OnDeselect(v);
            }
            selected.Clear();
        }

        internal void Add(T v)
        {
            if (!selected.Contains(v))
            {
                selected.Add(v);
                OnSelect(v);
            }
        }
    }
}

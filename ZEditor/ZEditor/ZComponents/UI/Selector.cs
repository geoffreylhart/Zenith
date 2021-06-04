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
            RegisterListener(new InputListener(Trigger.PlainLeftMouseClick, x =>
            {
                T selectedItem = indexSelectionProvider.GetSelectedIndex();
                foreach (var v in this.selected)
                {
                    if (!v.Equals(selectedItem))
                    {
                        OnDeselect(v);
                    }
                }
                if (!this.selected.Contains(selectedItem))
                {
                    OnSelect(selectedItem);
                }
                this.selected.Clear();
                this.selected.Add(selectedItem);
            }));
            RegisterListener(new InputListener(Trigger.ShiftLeftMouseClick, x =>
            {
                T selectedItem = indexSelectionProvider.GetSelectedIndex();
                if (this.selected.Contains(selectedItem))
                {
                    OnDeselect(selectedItem);
                    this.selected.Remove(selectedItem);
                }
                else
                {
                    OnSelect(selectedItem);
                    this.selected.Add(selectedItem);
                }
            }));
        }

        public override void Update(UIContext uiContext)
        {
            indexSelectionProvider.Update(uiContext);
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

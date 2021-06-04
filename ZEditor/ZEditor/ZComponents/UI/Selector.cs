using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private Action<T> AfterSelect;
        private Action<T> AfterDeselect;

        public Selector(IIndexSelectionProvider<T> indexSelectionProvider, Action<T> AfterSelect, Action<T> AfterDeselect)
        {
            this.indexSelectionProvider = indexSelectionProvider;
            this.AfterSelect = AfterSelect;
            this.AfterDeselect = AfterDeselect;
            RegisterListener(new InputListener(Trigger.PlainLeftMouseClick, x =>
            {
                T selectedItem = indexSelectionProvider.GetSelectedIndex();
                var removed = selected.Where(x => !x.Equals(selectedItem)).ToList();
                var alreadyContains = this.selected.Contains(selectedItem);
                this.selected.Clear();
                this.selected.Add(selectedItem);
                foreach (var v in removed) AfterDeselect(v);
                if (!alreadyContains) AfterSelect(selectedItem);
            }));
            RegisterListener(new InputListener(Trigger.ShiftLeftMouseClick, x =>
            {
                T selectedItem = indexSelectionProvider.GetSelectedIndex();
                if (this.selected.Contains(selectedItem))
                {
                    this.selected.Remove(selectedItem);
                    AfterDeselect(selectedItem);
                }
                else
                {
                    this.selected.Add(selectedItem);
                    AfterSelect(selectedItem);
                }
            }));
        }

        public override void Update(UIContext uiContext)
        {
            indexSelectionProvider.Update(uiContext);
        }

        internal void Clear()
        {
            var removed = selected.ToList();
            selected.Clear();
            foreach (var v in removed) AfterDeselect(v);
        }

        internal void Add(T v)
        {
            if (!selected.Contains(v))
            {
                selected.Add(v);
                AfterSelect(v);
            }
        }
    }
}

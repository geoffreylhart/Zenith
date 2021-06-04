using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;

namespace ZEditor.ZComponents.UI
{
    public interface IIndexSelectionProvider<T>
    {
        public T GetSelectedIndex();
        public void Update(UIContext uiContext);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;

namespace ZEditor.ZComponents.UI
{
    public interface IIndexSelectionProvider
    {
        public int GetSelectedIndex(UIContext uiContext); 
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZTemplates;

namespace ZEditor.ZComponents.UI
{
    public class CameraSelectionProvider : IIndexSelectionProvider
    {
        private IRayLookup<int> lookup;

        public CameraSelectionProvider(IRayLookup<int> lookup)
        {
            this.lookup = lookup;
        }

        public int GetSelectedIndex(UIContext uiContext)
        {
            return lookup.Get(uiContext.Camera.GetPosition(), uiContext.Camera.GetLookUnitVector(uiContext));
        }
    }
}

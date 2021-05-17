using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZTemplates;

namespace ZEditor.ZComponents.UI
{
    public class CameraSelectionProvider<T> : IIndexSelectionProvider<T>
    {
        private IRayLookup<T> lookup;

        public CameraSelectionProvider(IRayLookup<T> lookup)
        {
            this.lookup = lookup;
        }

        public T GetSelectedIndex(UIContext uiContext)
        {
            return lookup.Get(uiContext.Camera.GetPosition(), uiContext.Camera.GetLookUnitVector(uiContext));
        }
    }
}

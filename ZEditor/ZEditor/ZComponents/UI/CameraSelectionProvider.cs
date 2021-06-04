using Microsoft.Xna.Framework;
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
        private Vector3? position = null;
        private Vector3? lookUnitVector = null;

        public CameraSelectionProvider(IRayLookup<T> lookup)
        {
            this.lookup = lookup;
        }

        public T GetSelectedIndex()
        {
            return lookup.Get(position.Value, lookUnitVector.Value);
        }

        public void Update(UIContext uiContext)
        {
            position = uiContext.Camera.GetPosition();
            lookUnitVector = uiContext.Camera.GetLookUnitVector(uiContext);
        }
    }
}

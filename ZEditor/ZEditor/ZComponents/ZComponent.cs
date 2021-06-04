using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.UI;
using ZEditor.ZControl;

namespace ZEditor.ZManage
{
    public class ZComponent
    {
        private static ZComponent focusedObject = null;
        private List<InputListener> listeners = new List<InputListener>();
        private static List<InputListener> globalListeners = new List<InputListener>();
        public void RegisterListener(InputListener listener)
        {
            listeners.Add(listener);
        }
        public void RegisterGlobalListener(InputListener listener)
        {
            globalListeners.Add(listener);
        }
        public void UnregisterListener(InputListener listener)
        {
            listeners.Remove(listener);
        }
        public static void NotifyListeners(UIContext uiContext)
        {
            foreach (var listener in globalListeners)
            {
                uiContext.CheckListener(listener);
            }
            NotifyListenersAndChildren(uiContext, focusedObject);
        }

        private static void NotifyListenersAndChildren(UIContext uiContext, ZComponent obj)
        {
            if (obj != null)
            {
                foreach (var listener in obj.listeners.ToList())
                {
                    uiContext.CheckListener(listener);
                }
            }
            if (obj is ZGameObject)
            {
                foreach (var child in ((ZGameObject)obj).children)
                {
                    NotifyListenersAndChildren(uiContext, child);
                }
            }
        }

        public void Focus()
        {
            focusedObject = this;
        }
        public ZComponent GetFocus()
        {
            return focusedObject;
        }
        public virtual void Draw(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection) { }
        public virtual void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection) { }
        public virtual void Load(StreamReader reader, GraphicsDevice graphics) { }
        public virtual void Save(IndentableStreamWriter writer) { }
        public virtual void Update(UIContext uiContext) { }
        public virtual BoundingBox GetBoundingBox() { return new BoundingBox(); }
    }
}

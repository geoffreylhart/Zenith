using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZControl;

namespace ZEditor.ZManage
{
    public class ZGameObject : ZComponent
    {
        public List<ZComponent> children = new List<ZComponent>();
        public void Register(params ZComponent[] children)
        {
            this.children.AddRange(children);
        }

        public override void Draw(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            foreach (var child in children) child.Draw(graphics, world, view, projection);
        }

        public override void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            foreach (var child in children) child.DrawDebug(graphics, world, view, projection);
        }
        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            foreach (var child in children) child.Load(reader, graphicsDevice);
        }
        public override void Save(IndentableStreamWriter writer)
        {
            foreach (var child in children) child.Save(writer);
        }
        public override void Update(UIContext uiContext)
        {
            foreach (var child in children) child.Update(uiContext);
        }
    }
}

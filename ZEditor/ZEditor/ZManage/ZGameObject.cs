using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZManage
{
    public class ZGameObject : ZComponent
    {
        private List<ZComponent> children = new List<ZComponent>();
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
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZManage
{
    public class ZComponent
    {
        public virtual void Draw(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection) { }
        public virtual void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection) { }
    }
}

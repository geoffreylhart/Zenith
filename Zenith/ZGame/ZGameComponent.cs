using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Zenith.ZGraphics;

namespace Zenith.ZGame
{
    public class ZGameComponent
    {
        public virtual void Update(GraphicsDevice graphicsDevice, GameTime gameTime) { }
        public virtual void InitDraw(RenderContext renderContext) { }
        public virtual void Draw(RenderContext renderContext, GameTime gameTime) { }
    }
}

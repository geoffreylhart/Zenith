using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    interface IGraphicsBuffer : IDisposable
    {
        // don't actually draw anything to the caller, just initialize your own internal texture draws (only call this once per frame)
        void InitDraw(RenderContext context);
        // draw to the caller
        void Draw(RenderContext context);
        Texture2D GetImage(GraphicsDevice graphicsDevice);
    }
}

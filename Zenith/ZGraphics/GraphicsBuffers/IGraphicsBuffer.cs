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
        void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom);
        Texture2D GetImage(GraphicsDevice graphicsDevice);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    public class DebugBuffer : IGraphicsBuffer
    {
        public void InitDraw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
        }

        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom, RenderTargetBinding[] targets)
        {
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}

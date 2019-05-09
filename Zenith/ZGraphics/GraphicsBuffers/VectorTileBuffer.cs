using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.PrimitiveBuilder;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    class VectorTileBuffer : IGraphicsBuffer
    {
        List<BasicVertexBuffer> buffers = new List<BasicVertexBuffer>();

        public VectorTileBuffer()
        {
        }

        public VectorTileBuffer(GraphicsDevice graphicsDevice, List<VertexPositionColor> vectors, Sector sector)
        {
        }

        public void Dispose()
        {
            foreach (var buffer in buffers) buffer.Dispose();
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                var buffer = buffers[i];
                GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
                Matrix projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
                buffer.Draw(graphicsDevice, projection);
                graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, graphicsDevice.Viewport.MaxDepth, 0);
            }
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            Draw(newTarget, 0, 512, 0, 512, 0);
            return newTarget;
        }

        internal void Add(GraphicsDevice graphicsDevice, BasicVertexBuffer buffer, Sector sector)
        {
            buffers.Add(buffer);
        }
    }
}

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
        List<VertexBuffer> buffers = new List<VertexBuffer>();
        List<bool> isTrianglesList = new List<bool>();

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
                var basicEffect = new BasicEffect(graphicsDevice);
                basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
                basicEffect.VertexColorEnabled = true;
                graphicsDevice.SetVertexBuffer(buffer);
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    if (isTrianglesList[i])
                    {
                        graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, buffer.VertexCount - 2);
                    }
                    else
                    {
                        graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, buffer.VertexCount / 2);
                    }
                }
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

        internal void Add(GraphicsDevice graphicsDevice, List<VertexPositionColor> list, Sector sector, bool isTriangles)
        {
            if (list.Count > 0)
            {
                VertexBuffer buffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, list.Count, BufferUsage.WriteOnly);
                buffer.SetData(list.ToArray());
                buffers.Add(buffer);
                isTrianglesList.Add(isTriangles);
            }
        }
    }
}

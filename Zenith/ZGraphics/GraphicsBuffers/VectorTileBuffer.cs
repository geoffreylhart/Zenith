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
        VertexBuffer buffer = null;

        public VectorTileBuffer(GraphicsDevice graphicsDevice, List<VertexPositionColor> vectors, Sector sector)
        {
            if (vectors.Count > 0)
            {
                buffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, vectors.Count, BufferUsage.WriteOnly);
                buffer.SetData(vectors.ToArray());
            }
        }

        public void Dispose()
        {
            if (buffer != null) buffer.Dispose();
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            if (buffer != null)
            {
                GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
                var basicEffect = new BasicEffect(graphicsDevice);
                basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000); // TODO: figure out if flip was appropriate
                basicEffect.VertexColorEnabled = true;
                graphicsDevice.SetVertexBuffer(buffer);
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, buffer.VertexCount / 2);
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
    }
}

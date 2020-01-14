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
        private ISector sector;

        public VectorTileBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            this.sector = sector;
            buffers.Insert(0, GenerateWaterBuffer(graphicsDevice, this.sector));
        }

        public void Dispose()
        {
            foreach (var buffer in buffers) buffer.Dispose();
        }

        public void InitDraw(RenderContext context)
        {
        }

        public void Draw(RenderContext context)
        {
            if (context.layerPass == RenderContext.LayerPass.MAIN_PASS)
            {
                foreach (var buffer in buffers)
                {
                    buffer.Draw(context);
                    context.graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, context.graphicsDevice.Viewport.MaxDepth, 0);
                }
            }
        }

        // sure
        private BasicVertexBuffer GenerateWaterBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            Vector2d topLeft = new Vector2d(0, 0);
            Vector2d topRight = new Vector2d(1, 0);
            Vector2d bottomLeft = new Vector2d(0, 1);
            Vector2d bottomRight = new Vector2d(1, 1);
            vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)topRight.X, (float)topRight.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, 0), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomLeft.X, (float)bottomLeft.Y, 0), Pallete.OCEAN_BLUE));
            return new BasicVertexBuffer(graphicsDevice, vertices, PrimitiveType.TriangleList);
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        internal void Add(GraphicsDevice graphicsDevice, BasicVertexBuffer buffer, ISector sector)
        {
            buffers.Add(buffer);
        }
    }
}

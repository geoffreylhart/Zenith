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

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            foreach (var buffer in buffers)
            {
                GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
                Matrix projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
                buffer.Draw(graphicsDevice, projection);
                graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, graphicsDevice.Viewport.MaxDepth, 0);
            }
        }

        // sure
        private BasicVertexBuffer GenerateWaterBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            Vector2d topLeft = new Vector2d(sector.X * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d topRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d bottomLeft = new Vector2d(sector.X * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            Vector2d bottomRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, -10f), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)topRight.X, (float)topRight.Y, -10f), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, -10f), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, -10f), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, -10f), Pallete.OCEAN_BLUE));
            vertices.Add(new VertexPositionColor(new Vector3((float)bottomLeft.X, (float)bottomLeft.Y, -10f), Pallete.OCEAN_BLUE));
            return new BasicVertexBuffer(graphicsDevice, vertices, PrimitiveType.TriangleList);
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            Vector2d topLeft = new Vector2d(sector.X * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d bottomRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            Draw(newTarget, topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y, 0);
            return newTarget;
        }

        internal void Add(GraphicsDevice graphicsDevice, BasicVertexBuffer buffer, ISector sector)
        {
            buffers.Add(buffer);
        }
    }
}

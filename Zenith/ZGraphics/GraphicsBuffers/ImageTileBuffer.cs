using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.PrimitiveBuilder;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    // just an image of a sector
    class ImageTileBuffer : IGraphicsBuffer
    {
        VertexIndiceBuffer buffer;

        public ImageTileBuffer(GraphicsDevice graphicsDevice, Texture2D rendered, ISector sector)
        {
            buffer = new VertexIndiceBuffer();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            Vector2d topLeft = new Vector2d(sector.X * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d topRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d bottomLeft = new Vector2d(sector.X * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            Vector2d bottomRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            // TODO: everything is upside down, sadly
            vertices.Add(new VertexPositionNormalTexture(new Vector3((float)topLeft.X, (float)topLeft.Y, -10f), new Vector3(0, 0, 1), new Vector2(0, 1)));
            vertices.Add(new VertexPositionNormalTexture(new Vector3((float)topRight.X, (float)topRight.Y, -10f), new Vector3(0, 0, 1), new Vector2(1, 1)));
            vertices.Add(new VertexPositionNormalTexture(new Vector3((float)bottomLeft.X, (float)bottomLeft.Y, -10f), new Vector3(0, 0, 1), new Vector2(0, 0)));
            vertices.Add(new VertexPositionNormalTexture(new Vector3((float)bottomRight.X, (float)bottomRight.Y, -10f), new Vector3(0, 0, 1), new Vector2(1, 0)));
            List<int> indices = new List<int>() { 0, 1, 3, 0, 3, 2 };
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            buffer.indices.SetData(indices.ToArray());
            buffer.texture = rendered;
        }

        public void Dispose()
        {
            buffer.Dispose();
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            BasicEffect basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
            basicEffect.Texture = buffer.texture;
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.Indices = buffer.indices;
                graphicsDevice.SetVertexBuffer(buffer.vertices);
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
            }
            graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, graphicsDevice.Viewport.MaxDepth, 0);
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            return buffer.texture;
        }
    }
}

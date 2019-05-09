using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.ZGraphics
{
    class BasicVertexBuffer : IDisposable
    {
        public VertexBuffer vertices;
        public IndexBuffer indices;
        private PrimitiveType primitiveType;
        private GraphicsDevice graphicsDevice;
        private Texture2D texture;
        private PrimitiveType triangleList;

        public BasicVertexBuffer(GraphicsDevice graphicsDevice, List<VertexPositionColor> vertices, PrimitiveType primitiveType)
        {
            if (vertices.Count > 0)
            {
                this.vertices = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                this.vertices.SetData(vertices.ToArray());
                this.primitiveType = primitiveType;
            }
        }

        public BasicVertexBuffer(GraphicsDevice graphicsDevice, List<int> indices, List<VertexPositionColor> vertices, PrimitiveType primitiveType)
        {
            if (vertices.Count > 0)
            {
                this.vertices = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                this.vertices.SetData(vertices.ToArray());
                this.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                this.indices.SetData(indices.ToArray());
                this.primitiveType = primitiveType;
            }
        }

        public BasicVertexBuffer(GraphicsDevice graphicsDevice, List<int> indices, List<VertexPositionTexture> vertices, Texture2D texture, PrimitiveType primitiveType)
        {
            if (vertices.Count > 0)
            {
                this.vertices = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                this.vertices.SetData(vertices.ToArray());
                this.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                this.indices.SetData(indices.ToArray());
                this.texture = texture;
                this.primitiveType = primitiveType;
            }
        }

        public void Dispose()
        {
            if (vertices != null) vertices.Dispose();
            if (indices != null) indices.Dispose();
        }

        public void Draw(GraphicsDevice graphicsDevice, Matrix projection)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = projection;
            if (texture == null)
            {
                basicEffect.VertexColorEnabled = true;
            }
            else
            {
                basicEffect.Texture = texture;
                basicEffect.TextureEnabled = true;
            }
            if (vertices == null) return;
            if (indices != null) graphicsDevice.Indices = indices;
            graphicsDevice.SetVertexBuffer(vertices);
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                if (indices != null)
                {
                    switch (primitiveType)
                    {
                        case PrimitiveType.LineList:
                            graphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, indices.IndexCount / 2);
                            return;
                        case PrimitiveType.LineStrip:
                            graphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, indices.IndexCount - 1);
                            return;
                        case PrimitiveType.TriangleList:
                            graphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, indices.IndexCount / 3);
                            return;
                        case PrimitiveType.TriangleStrip:
                            graphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, indices.IndexCount - 2);
                            return;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    switch (primitiveType)
                    {
                        case PrimitiveType.LineList:
                            graphicsDevice.DrawPrimitives(primitiveType, 0, vertices.VertexCount / 2);
                            return;
                        case PrimitiveType.LineStrip:
                            graphicsDevice.DrawPrimitives(primitiveType, 0, vertices.VertexCount - 1);
                            return;
                        case PrimitiveType.TriangleList:
                            graphicsDevice.DrawPrimitives(primitiveType, 0, vertices.VertexCount / 3);
                            return;
                        case PrimitiveType.TriangleStrip:
                            graphicsDevice.DrawPrimitives(primitiveType, 0, vertices.VertexCount - 2);
                            return;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
}

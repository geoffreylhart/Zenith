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
        private bool textureWrap;
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

        public BasicVertexBuffer(GraphicsDevice graphicsDevice, List<int> indices, List<VertexPositionTexture> vertices, Texture2D texture, bool textureWrap, PrimitiveType primitiveType)
        {
            if (vertices.Count > 0)
            {
                this.vertices = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                this.vertices.SetData(vertices.ToArray());
                this.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                this.indices.SetData(indices.ToArray());
                this.texture = texture;
                this.textureWrap = textureWrap;
                this.primitiveType = primitiveType;
            }
        }

        public BasicVertexBuffer(GraphicsDevice graphicsDevice, List<int> indices, List<VertexPositionNormalTexture> vertices, Texture2D texture, bool textureWrap, PrimitiveType primitiveType)
        {
            if (vertices.Count > 0)
            {
                this.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                this.vertices.SetData(vertices.ToArray());
                this.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                this.indices.SetData(indices.ToArray());
                this.texture = texture;
                this.textureWrap = textureWrap;
                this.primitiveType = primitiveType;
            }
        }

        public void Dispose()
        {
            if (vertices != null) vertices.Dispose();
            if (indices != null) indices.Dispose();
        }
        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, RenderTargetBinding[] targets)
        {
            Draw(graphicsDevice, basicEffect, targets, primitiveType, texture, null);
        }

        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, RenderTargetBinding[] targets, PrimitiveType drawType, Texture2D drawTexture, Vector3? color)
        {
            Effect effect;
            if (targets == Game1.RENDER_BUFFER || targets == Game1.TREE_DENSITY_BUFFER || targets == Game1.GRASS_DENSITY_BUFFER)
            {
                BasicEffect clone = (BasicEffect)basicEffect.Clone();
                if (drawTexture == null)
                {
                    if (color == null)
                    {
                        clone.VertexColorEnabled = true;
                    }
                    else
                    {
                        clone.VertexColorEnabled = false;
                        clone.DiffuseColor = color.Value;
                    }
                }
                else
                {
                    if (textureWrap)
                    {
                        graphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                    }
                    else
                    {
                        graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                    }
                    clone.Texture = drawTexture;
                    clone.TextureEnabled = true;
                }
                effect = clone;
            }
            else if (targets == Game1.G_BUFFER)
            {
                if (drawTexture == null)
                {
                    if (color == null)
                    {
                        effect = GlobalContent.DeferredBasicColorShader;
                    }
                    else
                    {
                        effect = GlobalContent.DeferredBasicDiffuseShader;
                        effect.Parameters["DiffuseColor"].SetValue(new Vector4(color.Value, 1));
                    }
                }
                else
                {
                    effect = GlobalContent.DeferredBasicTextureShader;
                    if (textureWrap)
                    {
                        graphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                    }
                    else
                    {
                        graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                    }
                    effect.Parameters["Texture"].SetValue(drawTexture);
                }
                effect.Parameters["WVP"].SetValue(basicEffect.World * basicEffect.View * basicEffect.Projection);
            }
            else
            {
                throw new NotImplementedException();
            }
            if (vertices == null) return;
            if (indices != null) graphicsDevice.Indices = indices;
            graphicsDevice.SetVertexBuffer(vertices);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                if (indices != null)
                {
                    switch (drawType)
                    {
                        case PrimitiveType.LineList:
                            graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount / 2);
                            return;
                        case PrimitiveType.LineStrip:
                            graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount - 1);
                            return;
                        case PrimitiveType.TriangleList:
                            graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount / 3);
                            return;
                        case PrimitiveType.TriangleStrip:
                            graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount - 2);
                            return;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    switch (drawType)
                    {
                        case PrimitiveType.LineList:
                            graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount / 2);
                            return;
                        case PrimitiveType.LineStrip:
                            graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount - 1);
                            return;
                        case PrimitiveType.TriangleList:
                            graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount / 3);
                            return;
                        case PrimitiveType.TriangleStrip:
                            graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount - 2);
                            return;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
}

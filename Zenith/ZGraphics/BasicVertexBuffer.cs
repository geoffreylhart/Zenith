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
        public Texture2D texture;
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
        public void Draw(RenderContext context)
        {
            Draw(context, primitiveType, texture, null);
        }

        public void Draw(RenderContext context, PrimitiveType drawType, Texture2D drawTexture, Vector3? color)
        {
            Effect effect;
            if ((context.layerPass == RenderContext.LayerPass.MAIN_PASS && !Game1.DEFERRED_RENDERING) || context.layerPass == RenderContext.LayerPass.TREE_DENSITY_PASS || context.layerPass == RenderContext.LayerPass.GRASS_DENSITY_PASS)
            {
                BasicEffect basicEffect = new BasicEffect(context.graphicsDevice);
                basicEffect.World = context.WVP.toMatrix();
                if (drawTexture == null)
                {
                    if (color == null)
                    {
                        basicEffect.VertexColorEnabled = true;
                    }
                    else
                    {
                        basicEffect.VertexColorEnabled = false;
                        basicEffect.DiffuseColor = color.Value;
                    }
                }
                else
                {
                    if (textureWrap)
                    {
                        context.graphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                    }
                    else
                    {
                        context.graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                    }
                    basicEffect.Texture = drawTexture;
                    basicEffect.TextureEnabled = true;
                }
                effect = basicEffect;
            }
            else if (context.layerPass == RenderContext.LayerPass.MAIN_PASS && Game1.DEFERRED_RENDERING)
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
                        context.graphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                    }
                    else
                    {
                        context.graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                    }
                    effect.Parameters["Texture"].SetValue(drawTexture);
                }
                effect.Parameters["WVP"].SetValue(context.WVP.toMatrix());
            }
            else
            {
                throw new NotImplementedException();
            }
            if (vertices == null) return;
            if (indices != null) context.graphicsDevice.Indices = indices;
            context.graphicsDevice.SetVertexBuffer(vertices);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                if (indices != null)
                {
                    switch (drawType)
                    {
                        case PrimitiveType.LineList:
                            context.graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount / 2);
                            return;
                        case PrimitiveType.LineStrip:
                            context.graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount - 1);
                            return;
                        case PrimitiveType.TriangleList:
                            context.graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount / 3);
                            return;
                        case PrimitiveType.TriangleStrip:
                            context.graphicsDevice.DrawIndexedPrimitives(drawType, 0, 0, indices.IndexCount - 2);
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
                            context.graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount / 2);
                            return;
                        case PrimitiveType.LineStrip:
                            context.graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount - 1);
                            return;
                        case PrimitiveType.TriangleList:
                            context.graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount / 3);
                            return;
                        case PrimitiveType.TriangleStrip:
                            context.graphicsDevice.DrawPrimitives(drawType, 0, vertices.VertexCount - 2);
                            return;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
}

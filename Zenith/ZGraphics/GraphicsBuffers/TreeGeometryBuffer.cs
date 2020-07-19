using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    class TreeGeometryBuffer : IGraphicsBuffer
    {
        static VertexIndiceBuffer buffer; // contains tons of squares
        private static int REZ = 2048;

        public TreeGeometryBuffer(GraphicsDevice graphicsDevice)
        {
            // make that square, sure
            if (buffer == null)
            {
                buffer = new VertexIndiceBuffer();
                List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
                List<int> indices = new List<int>();
                // TODO: are all of these names wrong everywhere? the topleft etc?
                int size = 256;
                Random rand = new Random();
                for (int i = 0; i < size * size; i++)
                {
                    int x = i % size;
                    int y = i / size;
                    Vector3 randVec = new Vector3((float)rand.NextDouble() / 2 - 0.25f, (float)rand.NextDouble() / 2 - 0.25f, 0) / size;
                    Vector3 topLeft = new Vector3((x + 0.5f) / size, (y + 0.5f) / size, 0) + randVec;
                    Vector3 topRight = new Vector3((x + 0.5f) / size, (y + 0.5f) / size, 0) + randVec;
                    Vector3 bottomLeft = new Vector3((x + 0.5f) / size, (y + 0.5f) / size, 0) + randVec;
                    Vector3 bottomRight = new Vector3((x + 0.5f) / size, (y + 0.5f) / size, 0) + randVec;
                    vertices.Add(new VertexPositionTexture(topLeft, new Vector2(0, 0)));
                    vertices.Add(new VertexPositionTexture(topRight, new Vector2(1, 0)));
                    vertices.Add(new VertexPositionTexture(bottomLeft, new Vector2(0, 1)));
                    vertices.Add(new VertexPositionTexture(bottomRight, new Vector2(1, 1)));
                    indices.Add(i * 4);
                    indices.Add(i * 4 + 1);
                    indices.Add(i * 4 + 3);
                    indices.Add(i * 4);
                    indices.Add(i * 4 + 3);
                    indices.Add(i * 4 + 2);
                }
                if (graphicsDevice != null)
                {
                    buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                    buffer.vertices.SetData(vertices.OrderBy(x => x.Position.Y).ToArray());
                    buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                    buffer.indices.SetData(indices.ToArray());
                }
            }
        }

        public void Dispose()
        {
            // buffer.Dispose(); // DON'T dispose of the static buffer, duh!
        }

        public void InitDraw(RenderContext context)
        {
        }

        public void Draw(RenderContext context)
        {
            bool actuallyDeferred = context.deferred.HasValue ? context.deferred.Value : Game1.DEFERRED_RENDERING;
            if (context.layerPass == RenderContext.LayerPass.MAIN_PASS)
            {
                // first grass
                int size = 64;
                var effect = actuallyDeferred ? GlobalContent.DeferredTreeGeometryShader : GlobalContent.TreeGeometryShader;
                effect.Parameters["TreeExtraPH"].SetValue((float)context.treeExtraPH);
                effect.Parameters["TreeTexture"].SetValue(GlobalContent.Grass);
                effect.Parameters["Texture"].SetValue(context.grassLayer != null ? context.grassLayer : Game1.GRASS_DENSITY_BUFFER[0].RenderTarget);
                effect.Parameters["TreeVariance"].SetValue(new Vector2((float)0.5, (float)0.5));
                effect.Parameters["TreeSize"].SetValue(2f);
                effect.Parameters["TreeCenter"].SetValue(new Vector2((float)0.5, (float)1));
                effect.Parameters["Resolution"].SetValue(256f);
                effect.Parameters["TextureCount"].SetValue(4);

                context.graphicsDevice.Indices = buffer.indices;
                context.graphicsDevice.SetVertexBuffer(buffer.vertices);
                for (int i = 0; i < size * size; i++)
                {
                    int x = i % size;
                    int y = i / size;
                    if (x + 1 < context.minX * size || x > context.maxX * size || y + 1 < context.minY * size || y > context.maxY * size) continue;
                    Matrix WVP = Matrix.CreateScale(1f / size) * Matrix.CreateTranslation((float)x / size, (float)y / size, 0) * context.WVP.toMatrix();
                    effect.Parameters["WVP"].SetValue(WVP);
                    effect.Parameters["Inverse"].SetValue(Matrix.Invert(WVP));
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        context.graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                    }
                }
                // now trees
                size = 32;
                effect.Parameters["TreeTexture"].SetValue(GlobalContent.Tree);
                effect.Parameters["Texture"].SetValue(context.treeLayer != null ? context.treeLayer : Game1.TREE_DENSITY_BUFFER[0].RenderTarget);
                effect.Parameters["TreeVariance"].SetValue(new Vector2((float)0.5, (float)0.5));
                effect.Parameters["TreeSize"].SetValue(2f);
                effect.Parameters["TreeCenter"].SetValue(new Vector2((float)0.5, (float)1));
                effect.Parameters["Resolution"].SetValue(256f);
                effect.Parameters["TextureCount"].SetValue(1);

                context.graphicsDevice.Indices = buffer.indices;
                context.graphicsDevice.SetVertexBuffer(buffer.vertices);
                for (int i = 0; i < size * size; i++)
                {
                    int x = i % size;
                    int y = i / size;
                    if (x + 1 < context.minX * size || x > context.maxX * size || y + 1 < context.minY * size || y > context.maxY * size) continue;
                    Matrix WVP = Matrix.CreateScale(1f / size) * Matrix.CreateTranslation((float)x / size, (float)y / size, 0) * context.WVP.toMatrix();
                    effect.Parameters["WVP"].SetValue(WVP);
                    effect.Parameters["Inverse"].SetValue(Matrix.Invert(WVP));
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        context.graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                    }
                }
            }
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }
    }
}
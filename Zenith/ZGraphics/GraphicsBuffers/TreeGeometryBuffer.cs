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
        ISector sector;
        static VertexIndiceBuffer buffer; // contains tons of squares
        private static int REZ = 2048;
        BasicVertexBuffer beachBuffer;
        BasicVertexBuffer lakesBuffer;
        BasicVertexBuffer roadsBuffer;
        BasicVertexBuffer roadsBufferFat;
        BasicVertexBuffer beachCoastBuffer;
        BasicVertexBuffer lakesCoastBuffer;

        public TreeGeometryBuffer(GraphicsDevice graphicsDevice, BasicVertexBuffer beachBuffer, BasicVertexBuffer lakesBuffer, BasicVertexBuffer roadsBuffer, BasicVertexBuffer roadsBufferFat, BasicVertexBuffer beachCoastBuffer, BasicVertexBuffer lakesCoastBuffer, ISector sector)
        {
            this.beachBuffer = beachBuffer;
            this.lakesBuffer = lakesBuffer;
            this.roadsBuffer = roadsBuffer;
            this.roadsBufferFat = roadsBufferFat;
            this.beachCoastBuffer = beachCoastBuffer;
            this.lakesCoastBuffer = lakesCoastBuffer;
            this.sector = sector;
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
                buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
                buffer.vertices.SetData(vertices.OrderBy(x => x.Position.Y).ToArray());
                buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                buffer.indices.SetData(indices.ToArray());
            }
        }

        public void Dispose()
        {
            buffer.Dispose();
        }

        public void InitDraw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
        }

        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom, RenderTargetBinding[] targets)
        {
            if (maxX - minX > 0.1 || maxY - minY > 0.1) return;
            if (targets == Game1.TREE_DENSITY_BUFFER)
            {
                beachBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, null, new Vector3(1, 1, 1));
                lakesBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
                beachCoastBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, GlobalContent.BeachFlippedTreeDensity, new Vector3(1, 1, 1));
                lakesCoastBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, GlobalContent.BeachTreeDensity, new Vector3(0, 0, 0));
                roadsBufferFat.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, GlobalContent.RoadTreeDensity, new Vector3(0, 0, 0));
                roadsBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
            }
            if (targets == Game1.GRASS_DENSITY_BUFFER)
            {
                beachBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, null, new Vector3(1, 1, 1));
                lakesBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
                roadsBuffer.Draw(graphicsDevice, basicEffect, targets, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
            }
            if (targets == Game1.RENDER_BUFFER || targets == Game1.G_BUFFER)
            {
                // first grass
                int size = 64;
                var effect = targets == Game1.RENDER_BUFFER ? GlobalContent.TreeGeometryShader : GlobalContent.DeferredTreeGeometryShader;
                effect.Parameters["View"].SetValue(basicEffect.View);
                effect.Parameters["Projection"].SetValue(basicEffect.Projection);
                effect.Parameters["TreeTexture"].SetValue(GlobalContent.Grass);
                effect.Parameters["Texture"].SetValue(Game1.GRASS_DENSITY_BUFFER[0].RenderTarget);
                effect.Parameters["TreeVariance"].SetValue(new Vector2((float)0.5, (float)0.5));
                effect.Parameters["TreeSize"].SetValue(2f);
                effect.Parameters["TreeCenter"].SetValue(new Vector2((float)0.5, (float)1));
                effect.Parameters["Resolution"].SetValue(256f);
                effect.Parameters["TextureCount"].SetValue(4);

                graphicsDevice.Indices = buffer.indices;
                graphicsDevice.SetVertexBuffer(buffer.vertices);
                for (int i = 0; i < size * size; i++)
                {
                    int x = i % size;
                    int y = i / size;
                    if (x + 1 < minX * size || x > maxX * size || y + 1 < minY * size || y > maxY * size) continue;
                    Matrix world = Matrix.CreateScale(1f / size) * Matrix.CreateTranslation((float)x / size, (float)y / size, 0) * basicEffect.World;
                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["Inverse"].SetValue(Matrix.Invert(world * basicEffect.View * basicEffect.Projection));
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                    }
                }
                // now trees
                size = 32;
                effect.Parameters["TreeTexture"].SetValue(GlobalContent.Tree);
                effect.Parameters["Texture"].SetValue(Game1.TREE_DENSITY_BUFFER[0].RenderTarget);
                effect.Parameters["TreeVariance"].SetValue(new Vector2((float)0.5, (float)0.5));
                effect.Parameters["TreeSize"].SetValue(2f);
                effect.Parameters["TreeCenter"].SetValue(new Vector2((float)0.5, (float)1));
                effect.Parameters["Resolution"].SetValue(256f);
                effect.Parameters["TextureCount"].SetValue(1);

                graphicsDevice.Indices = buffer.indices;
                graphicsDevice.SetVertexBuffer(buffer.vertices);
                for (int i = 0; i < size * size; i++)
                {
                    int x = i % size;
                    int y = i / size;
                    if (x + 1 < minX * size || x > maxX * size || y + 1 < minY * size || y > maxY * size) continue;
                    Matrix world = Matrix.CreateScale(1f / size) * Matrix.CreateTranslation((float)x / size, (float)y / size, 0) * basicEffect.World;
                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["Inverse"].SetValue(Matrix.Invert(world * basicEffect.View * basicEffect.Projection));
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
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
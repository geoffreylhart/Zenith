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
    class TreeBuffer : IGraphicsBuffer
    {
        ISector sector;
        VertexIndiceBuffer buffer; // just a square
        private static int REZ = 2048;
        private Vector2[] textureOffsets;
        BasicVertexBuffer beachBuffer;
        BasicVertexBuffer lakesBuffer;
        BasicVertexBuffer roadsBuffer;
        BasicVertexBuffer roadsBufferFat;
        BasicVertexBuffer beachCoastBuffer;
        BasicVertexBuffer lakesCoastBuffer;

        public TreeBuffer(GraphicsDevice graphicsDevice, BasicVertexBuffer beachBuffer, BasicVertexBuffer lakesBuffer, BasicVertexBuffer roadsBuffer, BasicVertexBuffer roadsBufferFat, BasicVertexBuffer beachCoastBuffer, BasicVertexBuffer lakesCoastBuffer, ISector sector)
        {
            this.beachBuffer = beachBuffer;
            this.lakesBuffer = lakesBuffer;
            this.roadsBuffer = roadsBuffer;
            this.roadsBufferFat = roadsBufferFat;
            this.beachCoastBuffer = beachCoastBuffer;
            this.lakesCoastBuffer = lakesCoastBuffer;
            this.sector = sector;
            // make that square, sure
            buffer = new VertexIndiceBuffer();
            List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
            // TODO: are all of these names wrong everywhere? the topleft etc?
            Vector2d topLeft = new Vector2d(0, 0);
            Vector2d topRight = new Vector2d(1, 0);
            Vector2d bottomLeft = new Vector2d(0, 1);
            Vector2d bottomRight = new Vector2d(1, 1);
            vertices.Add(new VertexPositionTexture(new Vector3((float)topLeft.X, (float)topLeft.Y, 0), new Vector2(0, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)topRight.X, (float)topRight.Y, 0), new Vector2(1, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)bottomLeft.X, (float)bottomLeft.Y, 0), new Vector2(0, 1)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)bottomRight.X, (float)bottomRight.Y, 0), new Vector2(1, 1)));
            List<int> indices = new List<int>() { 0, 1, 3, 0, 3, 2 };
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            buffer.indices.SetData(indices.ToArray());
            List<Vector2> treePointList = new List<Vector2>();
            Random r = new Random(sector.GetHashCode() + 1);
            for (int i = 0; i < 5; i++)
            {
                Vector2 v = new Vector2((float)r.NextDouble() - 0.25f, (float)r.NextDouble() - 0.25f);
                treePointList.Add(v);
            }
            // for now, each point refers to the top left corner of the tree
            textureOffsets = new Vector2[9];
            for (int i = 0; i < 9; i++) textureOffsets[i] = new Vector2(i / 3 - 1, i % 3 - 1);
            textureOffsets = textureOffsets.OrderBy(x => x.Y).ToArray();
        }

        public void Dispose()
        {
            buffer.Dispose();
        }

        public void InitDraw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
        }

        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom, RenderTarget2D target)
        {
            if (target == Game1.TREE_DENSITY_BUFFER)
            {
                beachBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, null, new Vector3(1, 1, 1));
                lakesBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
                beachCoastBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, GlobalContent.BeachFlippedTreeDensity, new Vector3(1, 1, 1));
                lakesCoastBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, GlobalContent.BeachTreeDensity, new Vector3(0, 0, 0));
                roadsBufferFat.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, GlobalContent.RoadTreeDensity, new Vector3(0, 0, 0));
                roadsBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
            }
            if (target == Game1.GRASS_DENSITY_BUFFER)
            {
                beachBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, null, new Vector3(1, 1, 1));
                lakesBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
                roadsBuffer.Draw(graphicsDevice, basicEffect, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
            }
            if (target == Game1.ALBEDO_BUFFER)
            {
                var effect = GlobalContent.TreeShader;
                effect.Parameters["World"].SetValue(basicEffect.World);
                effect.Parameters["View"].SetValue(basicEffect.View);
                effect.Parameters["Projection"].SetValue(basicEffect.Projection);
                effect.Parameters["Inverse"].SetValue(Matrix.Invert(basicEffect.World * basicEffect.View * basicEffect.Projection));
                effect.Parameters["Texture"].SetValue(Game1.GRASS_DENSITY_BUFFER);
                effect.Parameters["TreeTexture"].SetValue(GlobalContent.Grass);
                effect.Parameters["TextureCount"].SetValue(4);
                effect.Parameters["TextureOffsets"].SetValue(textureOffsets);
                effect.Parameters["Resolution"].SetValue(REZ * 8f);
                effect.Parameters["TreeSize"].SetValue(2f);

                effect.Parameters["Min"].SetValue(new Vector2((float)minX, (float)minY));
                effect.Parameters["Max"].SetValue(new Vector2((float)maxX, (float)maxY));
                effect.Parameters["TreeCenter"].SetValue(new Vector2((float)0.5, (float)1));
                effect.Parameters["TreeVariance"].SetValue(new Vector2((float)0.5, (float)0.5));
                graphicsDevice.Indices = buffer.indices;
                graphicsDevice.SetVertexBuffer(buffer.vertices);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                }
                effect.Parameters["Texture"].SetValue(Game1.TREE_DENSITY_BUFFER);
                effect.Parameters["TreeTexture"].SetValue(GlobalContent.Tree);
                effect.Parameters["TextureCount"].SetValue(1);
                effect.Parameters["Resolution"].SetValue(REZ * 4f);
                effect.Parameters["TreeSize"].SetValue(2f);
                effect.Parameters["TreeCenter"].SetValue(new Vector2((float)0.5, (float)1));
                effect.Parameters["TreeVariance"].SetValue(new Vector2((float)0.5, (float)0.5));
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                }
            }
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }
    }
}

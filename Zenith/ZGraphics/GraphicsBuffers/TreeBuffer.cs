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
        RenderTarget2D treeTiles;
        VertexIndiceBuffer buffer; // just a square
        private static int REZ = 2048;
        private Vector2[] treePoints;
        private Vector2[] textureOffsets;

        public TreeBuffer(GraphicsDevice graphicsDevice, BasicVertexBuffer beachBuffer, BasicVertexBuffer lakesBuffer, BasicVertexBuffer roadsBuffer, ISector sector)
        {
            this.sector = sector;
            treeTiles = new RenderTarget2D(
                 graphicsDevice,
                 REZ,
                 REZ,
                 true,
                 graphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.None);
            graphicsDevice.SetRenderTarget(treeTiles);
            double minX = sector.X * sector.ZoomPortion;
            double maxX = (sector.X + 1) * sector.ZoomPortion;
            double minY = sector.Y * sector.ZoomPortion;
            double maxY = (sector.Y + 1) * sector.ZoomPortion;
            Matrix projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
            beachBuffer.Draw(graphicsDevice, projection, PrimitiveType.TriangleList, null, new Vector3(1, 1, 1));
            lakesBuffer.Draw(graphicsDevice, projection, PrimitiveType.TriangleList, null, new Vector3(0, 0, 0));
            roadsBuffer.Draw(graphicsDevice, projection, PrimitiveType.LineList, null, new Vector3(0, 0, 0));
            graphicsDevice.SetRenderTarget(null);
            // make that square, sure
            buffer = new VertexIndiceBuffer();
            List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
            // TODO: are all of these names wrong everywhere? the topleft etc?
            Vector2d topLeft = new Vector2d(sector.X * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d topRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d bottomLeft = new Vector2d(sector.X * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            Vector2d bottomRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            vertices.Add(new VertexPositionTexture(new Vector3((float)topLeft.X, (float)topLeft.Y, -10f), new Vector2(0, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)topRight.X, (float)topRight.Y, -10f), new Vector2(1, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)bottomLeft.X, (float)bottomLeft.Y, -10f), new Vector2(0, 1)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)bottomRight.X, (float)bottomRight.Y, -10f), new Vector2(1, 1)));
            List<int> indices = new List<int>() { 0, 1, 3, 0, 3, 2 };
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            buffer.indices.SetData(indices.ToArray());
            buffer.texture = treeTiles;
            List<Vector2> treePointList = new List<Vector2>();
            Random r = new Random(sector.GetHashCode() + 1);
            for (int i = 0; i < 5; i++)
            {
                Vector2 v = new Vector2((float)r.NextDouble() - 0.25f, (float)r.NextDouble() - 0.25f);
                treePointList.Add(v);
            }
            // for now, each point refers to the top left corner of the tree
            treePoints = treePointList.OrderBy(x => x.Y).ToArray();
            textureOffsets = new Vector2[] { new Vector2(-1f / REZ, -1f / REZ), new Vector2(-1f / REZ, 0), new Vector2(-1f / REZ, 1f / REZ), new Vector2(0, -1f / REZ), new Vector2(0, 0), new Vector2(0, 1f / REZ), new Vector2(1f / REZ, -1f / REZ), new Vector2(1f / REZ, 0), new Vector2(1f / REZ, 1f / REZ) };
            textureOffsets = textureOffsets.OrderBy(x => x.Y).ToArray();
        }

        public void Dispose()
        {
            treeTiles.Dispose();
            buffer.Dispose();
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            var effect = GlobalContent.TreeShader;
            effect.Parameters["World"].SetValue(Matrix.Identity);
            effect.Parameters["View"].SetValue(Matrix.Identity);
            effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000));
            effect.Parameters["Texture"].SetValue(treeTiles);
            effect.Parameters["TreeTexture"].SetValue(GlobalContent.Tree);
            //effect.Parameters["Offsets"].SetValue(treePoints);
            effect.Parameters["TextureOffsets"].SetValue(textureOffsets);
            effect.Parameters["Resolution"].SetValue((float)REZ);
            effect.Parameters["TreeSize"].SetValue(1f);
            // effect.Parameters["KeyColor"].SetValue(new Vector4(1, 1, 0, 1));
            graphicsDevice.Indices = buffer.indices;
            graphicsDevice.SetVertexBuffer(buffer.vertices);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
            }
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }
    }
}

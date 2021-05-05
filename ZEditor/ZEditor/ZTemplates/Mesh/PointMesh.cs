using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates.Mesh
{
    class PointMesh : AbstractMesh<VertexPositionColorTexture>
    {
        public override void DrawMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
        }
        public override void DrawDebugMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            Effect effect = GlobalContent.PointsShader;
            effect.Parameters["WVP"].SetValue(world * view * projection);
            effect.Parameters["PointSize"].SetValue(new Vector2(10f / graphicsDevice.Viewport.Width, 10f / graphicsDevice.Viewport.Height));
            graphicsDevice.SetVertexBuffer(buffer.vertexBuffer);
            graphicsDevice.Indices = buffer.indexBuffer;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indexBuffer.IndexCount / 3);
            }
        }

        public override bool FlippedAreEquivalent()
        {
            return false;
        }

        public override VertexPositionColorTexture MakeVertex(Vector3 position, Color color, int vertexNum, int[] item, Vector3[] positions)
        {
            return new VertexPositionColorTexture(position, color, new Vector2(vertexNum == 1 || vertexNum == 2 ? 1 : 0, vertexNum > 1 ? 1 : 0));
        }

        public override bool MergeAllVertices()
        {
            return false;
        }

        public override int NumPrimitives(int numVertices)
        {
            return numVertices - 2;
        }

        public override int[] PrimitiveIndexOffets(int primitiveNum)
        {
            return new int[] { 0, primitiveNum + 1, primitiveNum + 2 };
        }

        public override int VerticesPerVertex()
        {
            return 4;
        }

        public override bool WholeItemDependent()
        {
            return false;
        }
    }
}

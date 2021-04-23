using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZTemplates.Mesh
{
    class PointMesh : AbstractMesh<VertexPositionColorTexture>
    {
        public override bool FlippedAreEquivalent()
        {
            return false;
        }

        public override VertexPositionColorTexture MakeVertex(Vector3 position, int vertexNum, int[] item, Vector3[] positions)
        {
            return new VertexPositionColorTexture(position, Color.Black, new Vector2(vertexNum == 1 || vertexNum == 2 ? 1 : 0, vertexNum > 1 ? 1 : 0));
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

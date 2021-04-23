using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZTemplates.Mesh
{
    class LineMesh : AbstractMesh<VertexPositionColor>
    {
        public override bool FlippedAreEquivalent()
        {
            return true;
        }

        public override VertexPositionColor MakeVertex(Vector3 position, int vertexNum, int[] item, Vector3[] positions)
        {
            return new VertexPositionColor(position, Color.Black);
        }

        public override bool MergeAllVertices()
        {
            return true;
        }

        public override int NumPrimitives(int numVertices)
        {
            return 1;
        }

        public override int[] PrimitiveIndexOffets(int primitiveNum)
        {
            return new int[] { 0, 1 };
        }

        public override int VerticesPerVertex()
        {
            return 1;
        }

        public override bool WholeItemDependent()
        {
            return false;
        }
    }
}

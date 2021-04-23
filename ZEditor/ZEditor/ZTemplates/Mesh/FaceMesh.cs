using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZEditor.ZTemplates.Mesh
{
    class FaceMesh : AbstractMesh<VertexPositionNormalTexture>
    {
        public override bool FlippedAreEquivalent()
        {
            return false;
        }

        public override VertexPositionNormalTexture MakeVertex(Vector3 position, int vertexNum, int[] item, Vector3[] positions)
        {
            return new VertexPositionNormalTexture(position, CalculateNormal(item.Select(x => positions[x]).ToArray()), new Vector2(0, 0));
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
            return 1;
        }

        public override bool WholeItemDependent()
        {
            return true;
        }

        private Vector3 CalculateNormal(params Vector3[] vectors)
        {
            Vector3 normal = Vector3.Zero;
            // TODO: non-random averaging
            for (int i = 0; i < vectors.Length - 2; i++)
            {
                normal += Vector3.Cross(vectors[i + 2] - vectors[i], vectors[i + 1] - vectors[i + i]);
            }
            normal.Normalize();
            return normal;
        }
    }
}

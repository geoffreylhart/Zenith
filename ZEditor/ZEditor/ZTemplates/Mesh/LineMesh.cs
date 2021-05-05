using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZTemplates.Mesh
{
    class LineMesh : AbstractMesh<VertexPositionColor>
    {
        public override void DrawMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
        }
        public override void DrawDebugMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            BasicEffect effect = new BasicEffect(graphicsDevice);
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
            effect.VertexColorEnabled = true;
            buffer.Draw(PrimitiveType.LineList, graphicsDevice, effect);
        }

        public override bool FlippedAreEquivalent()
        {
            return true;
        }

        public override VertexPositionColor MakeVertex(Vector3 position, Color color, int vertexNum, int[] item)
        {
            return new VertexPositionColor(position, color);
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

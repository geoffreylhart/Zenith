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
        public override void DrawMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            BasicEffect effect = new BasicEffect(graphicsDevice);
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
            effect.DiffuseColor = new Vector3(1, 1, 1);
            effect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            effect.LightingEnabled = true;
            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.DiffuseColor = new Vector3(0.9f, 0.9f, 0.9f);
            var direction = new Vector3(2, -2, 10);
            direction.Normalize();
            effect.DirectionalLight0.Direction = direction;
            buffer.Draw(PrimitiveType.TriangleList, graphicsDevice, effect);
        }
        public override void DrawDebugMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            BasicEffect effect = new BasicEffect(graphicsDevice);
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
            effect.DiffuseColor = new Vector3(1, 1, 1);
            effect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            effect.LightingEnabled = true;
            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.DiffuseColor = new Vector3(0.9f, 0.9f, 0.9f);
            var direction = new Vector3(2, -2, 10);
            direction.Normalize();
            effect.DirectionalLight0.Direction = direction;
            effect.DirectionalLight1.Enabled = true;
            effect.DirectionalLight1.DiffuseColor = new Vector3(0.9f, 0.9f, 0.9f);
            var direction2 = new Vector3(-2, 2, -10);
            direction2.Normalize();
            effect.DirectionalLight1.Direction = direction2;
            buffer.Draw(PrimitiveType.TriangleList, graphicsDevice, effect);
        }

        public override bool FlippedAreEquivalent()
        {
            return false;
        }

        public override VertexPositionNormalTexture MakeVertex(Vector3 position, Color color, int vertexNum, int[] item)
        {
            return new VertexPositionNormalTexture(position, CalculateNormal(item.Select(x => vertexData.positions[x]).ToArray()), new Vector2(0, 0));
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZEditor.DataStructures;
using ZEditor.ZComponents.Data;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates.Mesh
{
    // let's just do this super inefficiently and go from there
    public class FaceMesh : ZComponent, IVertexObserver
    {
        public DynamicVertexIndexBuffer<VertexPositionNormalTexture> buffer;
        public VertexDataComponent vertexData;
        private HashSet<int[]> items = new HashSet<int[]>(new IntListEqualityComparer());

        public FaceMesh()
        {
            buffer = new DynamicVertexIndexBuffer<VertexPositionNormalTexture>();
        }

        public void AddItem(int[] item)
        {
            items.Add(item);
            RecalculateEverything();
        }

        public void RemoveItem(int[] item)
        {
            items.Remove(item);
            RecalculateEverything();
        }

        private void RecalculateEverything()
        {
            if (buffer != null) buffer.Dispose();
            buffer = new DynamicVertexIndexBuffer<VertexPositionNormalTexture>();
            int verticesAdded = 0;
            foreach (var item in items)
            {
                var normal = CalculateNormal(item.Select(x => vertexData.positions[x]).ToArray());
                var vertices = item.Select(x => new VertexPositionNormalTexture(vertexData.positions[x], normal, new Vector2(0, 0))).ToList();
                var indices = new List<int>();
                for (int i = 0; i < item.Length - 2; i++)
                {
                    indices.Add(verticesAdded);
                    indices.Add(verticesAdded + i + 1);
                    indices.Add(verticesAdded + i + 2);
                }
                buffer.AddVertices(vertices);
                buffer.AddIndices(indices);
                verticesAdded += vertices.Count();
            }
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

        public override void Draw(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
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

        public override void DrawDebug(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
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

        public void Add(int index, Vector3 v, Color color)
        {
        }

        public void Update(int index, Vector3 v, Color color)
        {
            RecalculateEverything();
        }
    }
}

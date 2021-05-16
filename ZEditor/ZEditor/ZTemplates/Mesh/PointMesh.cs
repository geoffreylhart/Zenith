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
    public class PointMesh : ZComponent, IVertexObserver
    {
        public DynamicVertexIndexBuffer<VertexPositionColorTexture> buffer;
        public VertexDataComponent vertexData;
        private HashSet<int> items = new HashSet<int>();

        public PointMesh()
        {
            buffer = new DynamicVertexIndexBuffer<VertexPositionColorTexture>();
        }

        public void AddItem(int item)
        {
            items.Add(item);
            RecalculateEverything();
        }

        public void RemoveItem(int item)
        {
            items.Remove(item);
            RecalculateEverything();
        }

        private void RecalculateEverything()
        {
            if (buffer != null) buffer.Dispose();
            buffer = new DynamicVertexIndexBuffer<VertexPositionColorTexture>();
            int verticesAdded = 0;
            foreach (var item in items)
            {
                var vertices = new List<VertexPositionColorTexture>();
                vertices.Add(new VertexPositionColorTexture(vertexData.positions[item], vertexData.colors[item], new Vector2(0, 0)));
                vertices.Add(new VertexPositionColorTexture(vertexData.positions[item], vertexData.colors[item], new Vector2(1, 0)));
                vertices.Add(new VertexPositionColorTexture(vertexData.positions[item], vertexData.colors[item], new Vector2(1, 1)));
                vertices.Add(new VertexPositionColorTexture(vertexData.positions[item], vertexData.colors[item], new Vector2(0, 1)));
                var indices = new List<int>();
                indices.Add(verticesAdded * 4);
                indices.Add(verticesAdded * 4 + 1);
                indices.Add(verticesAdded * 4 + 2);
                indices.Add(verticesAdded * 4);
                indices.Add(verticesAdded * 4 + 2);
                indices.Add(verticesAdded * 4 + 3);
                buffer.AddVertices(vertices);
                buffer.AddIndices(indices);
                verticesAdded++;
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
        }

        public override void DrawDebug(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            Effect effect = GlobalContent.PointsShader;
            effect.Parameters["WVP"].SetValue(world * view * projection);
            effect.Parameters["PointSize"].SetValue(new Vector2(10f / graphicsDevice.Viewport.Width, 10f / graphicsDevice.Viewport.Height));
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

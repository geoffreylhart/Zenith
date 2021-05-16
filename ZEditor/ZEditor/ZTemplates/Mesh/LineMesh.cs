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
    public class LineMesh : ZComponent, IVertexObserver
    {
        public DynamicVertexIndexBuffer<VertexPositionColor> buffer;
        public VertexDataComponent vertexData;
        private HashSet<int[]> items = new HashSet<int[]>(new ReversibleIntListEqualityComparer());

        public LineMesh()
        {
            buffer = new DynamicVertexIndexBuffer<VertexPositionColor>();
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
            buffer = new DynamicVertexIndexBuffer<VertexPositionColor>();
            Dictionary<int, int> verticesAdded = new Dictionary<int, int>();
            foreach (var item in items)
            {
                foreach(var v in item)
                {
                    if (!verticesAdded.ContainsKey(v))
                    {
                        buffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(vertexData.positions[v], vertexData.colors[v]) });
                        verticesAdded.Add(v, verticesAdded.Count);
                    }
                }
                var indices = item.Select(x => verticesAdded[x]).ToList();
                buffer.AddIndices(indices);
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
        }

        public override void DrawDebug(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            BasicEffect effect = new BasicEffect(graphicsDevice);
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
            effect.VertexColorEnabled = true;
            buffer.Draw(PrimitiveType.LineList, graphicsDevice, effect);
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

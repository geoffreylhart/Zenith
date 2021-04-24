using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using ZEditor.ZGraphics;

namespace ZEditor.ZTemplates.Mesh
{
    // an attempt to abstract out the logic of points/lines/faces
    abstract class AbstractMesh<T> where T : struct, IVertexType
    {
        private HashSet<ItemInfo> items;
        private Dictionary<int, HashSet<ItemInfo>> itemLookup;
        public VertexIndexBuffer buffer;

        private class ItemInfo
        {
            public int[] vertices;
            public int[] indices;
            public bool flippable;

            public override bool Equals(object obj)
            {
                ItemInfo that = (ItemInfo)obj;
                if (this.vertices.Length != that.vertices.Length) return false;
                for (int i = 0; i < this.vertices.Length; i++)
                {
                    if (this.vertices[i] != that.vertices[i])
                    {
                        if (!flippable) return false;
                        for (int j = 0; j < this.vertices.Length; j++)
                        {
                            if (this.vertices[j] != that.vertices[this.vertices.Length - 1 - j]) return false;
                        }
                        return true;
                    }
                }
                return true;
            }

            public override int GetHashCode()
            {
                int result = 17;
                foreach (var x in vertices) result = result * 23 + x;
                if (!flippable) return result;
                int result2 = 17;
                for (int i = 0; i < vertices.Length; i++) result2 = result2 * 23 + vertices[vertices.Length - 1 - i];
                return Math.Min(result, result2);
            }
        }

        public AbstractMesh()
        {
            itemLookup = new Dictionary<int, HashSet<ItemInfo>>();
            items = new HashSet<ItemInfo>();
        }

        public void AddItem(int[] itemIndices)
        {
            ItemInfo item = new ItemInfo() { vertices = itemIndices, indices = new int[itemIndices.Length * VerticesPerVertex()], flippable = FlippedAreEquivalent() };
            if (!items.Contains(item))
            {
                items.Add(item);
                foreach (int vertex in item.vertices)
                {
                    if (!itemLookup.ContainsKey(vertex)) itemLookup[vertex] = new HashSet<ItemInfo>();
                    itemLookup[vertex].Add(item);
                }
            }
        }

        public void Update(int vertexIndice, Vector3[] positions, Color[] colors)
        {
            foreach (var item in itemLookup[vertexIndice])
            {
                for (int i = 0; i < item.vertices.Length; i++)
                {
                    if (!WholeItemDependent() && item.vertices[i] != vertexIndice) continue;
                    T[] temp = new T[VerticesPerVertex()];
                    for (int j = 0; j < VerticesPerVertex(); j++)
                    {
                        temp[j] = MakeVertex(positions[item.vertices[i]], colors[item.vertices[i]], i * VerticesPerVertex() + j, item.vertices, positions);
                    }
                    buffer.vertexBuffer.SetData(new T().VertexDeclaration.VertexStride * item.indices[i], temp, 0, VerticesPerVertex(), new T().VertexDeclaration.VertexStride);
                }
                if (MergeAllVertices()) break;
            }
        }

        public VertexIndexBuffer MakeBuffer(Vector3[] positions, Color[] colors, GraphicsDevice graphicsDevice)
        {
            List<T> vertices = new List<T>();
            List<int> indices = new List<int>();
            Dictionary<int, int> verticesAdded = new Dictionary<int, int>();
            foreach (var item in items)
            {
                int[] itemReplacement = new int[item.vertices.Length * VerticesPerVertex()];
                for (int i = 0; i < itemReplacement.Length; i++)
                {
                    itemReplacement[i] = item.vertices[i / VerticesPerVertex()] * VerticesPerVertex() + i / item.vertices.Length;
                }
                for (int i = 0; i < NumPrimitives(itemReplacement.Length); i++)
                {
                    int[] indexOffsets = PrimitiveIndexOffets(i);
                    for (int j = 0; j < indexOffsets.Length; j++)
                    {
                        if (verticesAdded.ContainsKey(itemReplacement[indexOffsets[j]]))
                        {
                            indices.Add(verticesAdded[itemReplacement[indexOffsets[j]]]);
                        }
                        else
                        {
                            vertices.Add(MakeVertex(positions[itemReplacement[indexOffsets[j]] / VerticesPerVertex()], colors[itemReplacement[indexOffsets[j]] / VerticesPerVertex()], indexOffsets[j], itemReplacement, positions));
                            verticesAdded.Add(itemReplacement[indexOffsets[j]], vertices.Count - 1);
                            indices.Add(vertices.Count - 1);
                        }
                        item.indices[indexOffsets[j]] = indices.Last();
                    }
                }
                if (!MergeAllVertices()) verticesAdded = new Dictionary<int, int>();
            }
            var vertexBuffer = new VertexBuffer(graphicsDevice, new T().VertexDeclaration, vertices.Count, BufferUsage.None);
            vertexBuffer.SetData(vertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            indexBuffer.SetData(indices.ToArray());
            buffer = new VertexIndexBuffer(vertexBuffer, indexBuffer);
            return buffer;
        }


        public abstract int NumPrimitives(int numVertices);
        public abstract int[] PrimitiveIndexOffets(int primitiveNum);
        public abstract T MakeVertex(Vector3 position, Color color, int vertexNum, int[] item, Vector3[] positions);
        public abstract bool FlippedAreEquivalent();
        public abstract bool MergeAllVertices(); // not just vertices within a single item
        public abstract int VerticesPerVertex();
        public abstract bool WholeItemDependent();
    }
}

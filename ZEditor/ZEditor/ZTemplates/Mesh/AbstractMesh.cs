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
        private Dictionary<int, List<ItemInfo>> itemLookup;
        public VertexIndexBuffer buffer;

        private class ItemInfo
        {
            public int[] vertices;
            public int[] indices;
        }

        public AbstractMesh()
        {
            itemLookup = new Dictionary<int, List<ItemInfo>>();
            if (FlippedAreEquivalent())
            {
                items = new HashSet<ItemInfo>(new FlippableIntArrayComparer());
            }
            else
            {
                items = new HashSet<ItemInfo>(new IntArrayComparer());
            }
        }

        public void AddItem(int[] itemIndices)
        {
            ItemInfo item = new ItemInfo() { vertices = itemIndices, indices = new int[itemIndices.Length * VerticesPerVertex()] };
            if (!items.Contains(item))
            {
                items.Add(item);
                foreach (int vertex in item.vertices)
                {
                    if (!itemLookup.ContainsKey(vertex)) itemLookup[vertex] = new List<ItemInfo>();
                    itemLookup[vertex].Add(item);
                }
            }
        }

        public void Update(int vertexIndice, Vector3 newPosition, Vector3[] positions)
        {
            foreach (var item in itemLookup[vertexIndice])
            {
                for (int i = 0; i < item.vertices.Length; i++)
                {
                    T[] temp = new T[VerticesPerVertex()];
                    buffer.vertexBuffer.GetData(new T().VertexDeclaration.VertexStride * item.indices[i], temp, 0, VerticesPerVertex(), new T().VertexDeclaration.VertexStride);
                    for (int j = 0; j < VerticesPerVertex(); j++)
                    {
                        temp[j] = MakeVertex(positions[item.vertices[i]], i * VerticesPerVertex() + j, item.vertices, positions);
                    }
                    buffer.vertexBuffer.SetData(new T().VertexDeclaration.VertexStride * item.indices[i], temp, 0, VerticesPerVertex(), new T().VertexDeclaration.VertexStride);
                    if (!WholeItemDependent()) break;
                }
                if (MergeAllVertices()) break;
            }
        }

        public VertexIndexBuffer MakeBuffer(Vector3[] positions, GraphicsDevice graphicsDevice)
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
                            vertices.Add(MakeVertex(positions[itemReplacement[indexOffsets[j]] / VerticesPerVertex()], indexOffsets[j], itemReplacement, positions));
                            verticesAdded.Add(itemReplacement[indexOffsets[j]], vertices.Count - 1);
                            indices.Add(vertices.Count - 1);
                        }
                        item.indices[indexOffsets[j]] = indices.Last();
                    }
                }
                if (!MergeAllVertices()) verticesAdded = new Dictionary<int, int>();
            }
            var vertexBuffer = new VertexBuffer(graphicsDevice, new T().VertexDeclaration, vertices.Count, BufferUsage.None);
            vertexBuffer.SetData<T>(vertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            indexBuffer.SetData(indices.ToArray());
            buffer = new VertexIndexBuffer(vertexBuffer, indexBuffer);
            return buffer;
        }


        public abstract int NumPrimitives(int numVertices);
        public abstract int[] PrimitiveIndexOffets(int primitiveNum);
        public abstract T MakeVertex(Vector3 position, int vertexNum, int[] item, Vector3[] positions);
        public abstract bool FlippedAreEquivalent();
        public abstract bool MergeAllVertices(); // not just vertices within a single item
        public abstract int VerticesPerVertex();
        public abstract bool WholeItemDependent();

        private class IntArrayComparer : IEqualityComparer<ItemInfo>
        {
            public bool Equals(ItemInfo x, ItemInfo y)
            {
                if (x.vertices.Length != y.vertices.Length) return false;
                for (int i = 0; i < x.vertices.Length; i++)
                {
                    if (x.vertices[i] != y.vertices[i]) return false;
                }
                return true;
            }

            public int GetHashCode(ItemInfo obj)
            {
                int result = 17;
                foreach (var x in obj.vertices) result = result * 23 + x;
                return result;
            }
        }

        private class FlippableIntArrayComparer : IEqualityComparer<ItemInfo>
        {
            public bool Equals(ItemInfo x, ItemInfo y)
            {
                if (x.vertices.Length != y.vertices.Length) return false;
                for (int i = 0; i < x.vertices.Length; i++)
                {
                    if (x.vertices[i] != y.vertices[i])
                    {
                        for (int j = 0; j < x.vertices.Length; j++)
                        {
                            if (x.vertices[j] != y.vertices[x.vertices.Length - 1 - i]) return false;
                        }
                    }
                }
                return true;
            }

            public int GetHashCode(ItemInfo obj)
            {
                int result = 17;
                foreach (var x in obj.vertices) result = result * 23 + x;
                int result2 = 17;
                for (int i = 0; i < obj.vertices.Length; i++) result2 = result2 * 23 + obj.vertices[obj.vertices.Length - 1 - i];
                return Math.Min(result, result2);
            }
        }
    }
}

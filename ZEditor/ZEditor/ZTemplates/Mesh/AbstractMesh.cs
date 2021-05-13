using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using ZEditor.ZComponents.Data;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates.Mesh
{
    // an attempt to abstract out the logic of points/lines/faces
    abstract class AbstractMesh<T> : ZComponent, IVertexObserver where T : struct, IVertexType
    {
        private int vertexCount = 0;
        private int indexCount = 0;
        private List<ItemInfo> primitiveItemLookup = new List<ItemInfo>();
        private HashSet<ItemInfo> items;
        private Dictionary<int, HashSet<ItemInfo>> itemLookup;
        public DynamicVertexIndexBuffer<T> buffer;
        public VertexDataComponent vertexData;
        // TODO: remove if possible
        private Dictionary<int, int> verticesAdded = new Dictionary<int, int>();

        private class ItemInfo
        {
            public int[] vertices;
            public int[] indices;
            // the index index, jeez
            public List<int> primitiveIndices = new List<int>();
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
            buffer = new DynamicVertexIndexBuffer<T>();
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
                UpdateBuffersWithItem(item);
            }
        }

        public void RemoveItem(int[] itemIndices)
        {
            ItemInfo itemDummy = new ItemInfo() { vertices = itemIndices, flippable = FlippedAreEquivalent() };
            ItemInfo actualItem;
            if (items.TryGetValue(itemDummy, out actualItem))
            {
                items.Remove(actualItem);
                foreach (int vertex in actualItem.vertices)
                {
                    itemLookup[vertex].Remove(actualItem);
                    if (itemLookup[vertex].Count == 0)
                    {
                        // orphaned vertex
                        itemLookup.Remove(vertex);
                        throw new NotImplementedException();
                    }
                }
                for (int i = 0; i < actualItem.primitiveIndices.Count; i++)
                {
                    int deletingIndex = actualItem.primitiveIndices[i];
                    int lastPIndex = (primitiveItemLookup.Count - 1) * PrimitiveSize();
                    // take the "last" primitive and replace ours spot with it, then shrink total primitives
                    // TODO: this is insanity
                    var prims = primitiveItemLookup.Last().primitiveIndices;
                    int maxIndex = prims.IndexOf(lastPIndex);
                    prims[maxIndex] = deletingIndex;
                    primitiveItemLookup[deletingIndex / PrimitiveSize()] = primitiveItemLookup.Last();
                    primitiveItemLookup.RemoveAt(primitiveItemLookup.Count - 1);
                    buffer.SetIndices(deletingIndex, lastPIndex, PrimitiveSize());
                    buffer.ReduceIndices(PrimitiveSize());
                }
            }
        }

        private void UpdateBuffersWithItem(ItemInfo item)
        {
            List<T> vertices = new List<T>();
            List<int> indices = new List<int>();
            int[] itemReplacement = new int[item.vertices.Length * VerticesPerVertex()];
            for (int i = 0; i < itemReplacement.Length; i++)
            {
                itemReplacement[i] = item.vertices[i / VerticesPerVertex()] * VerticesPerVertex() + i / item.vertices.Length;
            }
            for (int i = 0; i < NumPrimitives(itemReplacement.Length); i++)
            {
                item.primitiveIndices.Add(indexCount);
                int[] indexOffsets = PrimitiveIndexOffets(i);
                primitiveItemLookup.Add(item);
                for (int j = 0; j < indexOffsets.Length; j++)
                {
                    if (verticesAdded.ContainsKey(itemReplacement[indexOffsets[j]]))
                    {
                        indices.Add(verticesAdded[itemReplacement[indexOffsets[j]]]);
                    }
                    else
                    {
                        vertices.Add(MakeVertex(vertexData.positions[itemReplacement[indexOffsets[j]] / VerticesPerVertex()], vertexData.colors[itemReplacement[indexOffsets[j]] / VerticesPerVertex()], indexOffsets[j], itemReplacement));
                        verticesAdded.Add(itemReplacement[indexOffsets[j]], vertexCount);
                        indices.Add(vertexCount);
                        vertexCount++;
                    }
                    indexCount++;
                    item.indices[indexOffsets[j]] = indices.Last();
                }
            }
            if (!MergeAllVertices()) verticesAdded = new Dictionary<int, int>();
            buffer.AddVertices(vertices);
            buffer.AddIndices(indices);
        }

        public abstract int NumPrimitives(int numVertices);
        public abstract int[] PrimitiveIndexOffets(int primitiveNum);
        public abstract int PrimitiveSize();
        public abstract T MakeVertex(Vector3 position, Color color, int vertexNum, int[] item);
        public abstract bool FlippedAreEquivalent();
        public abstract bool MergeAllVertices(); // not just vertices within a single item
        public abstract int VerticesPerVertex();
        public abstract bool WholeItemDependent();
        // note: this could maybe all be abstracted out, yeah?
        public abstract void DrawMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection);
        public abstract void DrawDebugMesh(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection);

        public override void Draw(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            DrawMesh(graphicsDevice, world, view, projection);
        }

        public override void DrawDebug(GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            DrawDebugMesh(graphicsDevice, world, view, projection);
        }

        public void Add(int index, Vector3 v, Color color)
        {
        }

        public void Update(int index, Vector3 v, Color color)
        {
            foreach (var item in itemLookup[index])
            {
                for (int i = 0; i < item.vertices.Length; i++)
                {
                    if (!WholeItemDependent() && item.vertices[i] != index) continue;
                    T[] temp = new T[VerticesPerVertex()];
                    for (int j = 0; j < VerticesPerVertex(); j++)
                    {
                        temp[j] = MakeVertex(vertexData.positions[item.vertices[i]], vertexData.colors[item.vertices[i]], i * VerticesPerVertex() + j, item.vertices);
                    }
                    buffer.SetVertices(item.indices[i], temp);
                }
                if (MergeAllVertices()) break;
            }
        }
    }
}

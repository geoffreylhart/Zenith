using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZGraphics
{
    // lazy and editable, and acts like an arraylist
    public class DynamicVertexIndexBuffer<T> : IDisposable where T : struct, IVertexType
    {
        private static double GROWTH_FACTOR = 1.5;
        private static double SHRINK_FACTOR = 0.3; // should be less than 1/GROWTH_FACTOR
        private static int MIN_SIZE = 10;
        private int vertexCount = 0;
        private int indexCount = 0;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private List<T> pendingVertices = new List<T>();
        private List<int> pendingIndices = new List<int>();

        public void Dispose()
        {
            if (vertexBuffer != null) vertexBuffer.Dispose();
            if (indexBuffer != null) indexBuffer.Dispose();
        }

        internal void Draw(PrimitiveType primitiveType, GraphicsDevice graphicsDevice, Effect effect)
        {
            int proposedVertexSize = Math.Max(vertexCount, MIN_SIZE);
            while (proposedVertexSize < vertexCount + pendingVertices.Count)
            {
                proposedVertexSize = (int)(proposedVertexSize * GROWTH_FACTOR);
            }
            if (vertexBuffer == null || proposedVertexSize > vertexBuffer.VertexCount)
            {
                T[] newVertexData = new T[proposedVertexSize];
                VertexBuffer newVertexBuffer = new VertexBuffer(graphicsDevice, new T().VertexDeclaration, newVertexData.Length, BufferUsage.None);
                if (vertexBuffer != null)
                {
                    vertexBuffer.GetData(newVertexData, 0, vertexCount);
                    vertexBuffer.Dispose();
                }
                for (int i = 0; i < pendingVertices.Count; i++)
                {
                    newVertexData[vertexCount + i] = pendingVertices[i];
                }
                vertexCount += pendingVertices.Count;
                pendingVertices.Clear();
                newVertexBuffer.SetData(newVertexData);
                vertexBuffer = newVertexBuffer;
            }
            int proposedIndexCount = Math.Max(indexCount, MIN_SIZE);
            while (proposedIndexCount < indexCount + pendingIndices.Count)
            {
                proposedIndexCount = (int)(proposedIndexCount * GROWTH_FACTOR);
            }
            if (indexBuffer == null || proposedIndexCount > indexBuffer.IndexCount)
            {
                int[] newIndexData = new int[proposedIndexCount];
                IndexBuffer newIndexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, newIndexData.Length, BufferUsage.None);
                if (indexBuffer != null)
                {
                    indexBuffer.GetData(newIndexData, 0, indexCount);
                    indexBuffer.Dispose();
                }
                for (int i = 0; i < pendingIndices.Count; i++)
                {
                    newIndexData[indexCount + i] = pendingIndices[i];
                }
                indexCount += pendingIndices.Count;
                pendingIndices.Clear();
                newIndexBuffer.SetData(newIndexData);
                indexBuffer = newIndexBuffer;
            }
            if (indexCount > 0)
            {
                graphicsDevice.SetVertexBuffer(vertexBuffer);
                graphicsDevice.Indices = indexBuffer;
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    switch (primitiveType)
                    {
                        case PrimitiveType.TriangleList:
                            graphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, indexCount / 3);
                            break;
                        case PrimitiveType.LineList:
                            graphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, indexCount / 2);
                            break;
                    }
                }
            }
        }

        public void AddVertices(List<T> vertices)
        {
            pendingVertices.AddRange(vertices);
        }

        public void AddIndices(List<int> indices)
        {
            pendingIndices.AddRange(indices);
        }

        public void SetVertices(int offset, T[] data)
        {
            if (vertexBuffer == null)
            {
                // all vertices update pending
                for (int i = 0; i < data.Length; i++)
                {
                    pendingVertices[i + offset] = data[i];
                }
            }
            else if (vertexBuffer.VertexCount < offset)
            {
                // all vertices update pending
                for (int i = 0; i < data.Length; i++)
                {
                    pendingVertices[i + offset - vertexBuffer.VertexCount] = data[i];
                }
            }
            else if (offset + data.Length <= vertexBuffer.VertexCount)
            {
                // all vertices go to buffer
                vertexBuffer.SetData(new T().VertexDeclaration.VertexStride * offset, data, 0, data.Length, new T().VertexDeclaration.VertexStride);
            }
            else
            {
                // split the vertices between the two
                throw new NotImplementedException();
            }
        }

        public void SetIndices(int offset, int[] data)
        {
            if (indexBuffer == null)
            {
                // all indices update pending
                for (int i = 0; i < data.Length; i++)
                {
                    pendingIndices[i + offset] = data[i];
                }
            }
            else if (indexBuffer.IndexCount < offset)
            {
                // all indices update pending
                for (int i = 0; i < data.Length; i++)
                {
                    pendingIndices[i + offset - indexBuffer.IndexCount] = data[i];
                }
            }
            else if (offset + data.Length <= indexBuffer.IndexCount)
            {
                // all vertices go to buffer
                indexBuffer.SetData(4 * offset, data, 0, data.Length);
            }
            else
            {
                // split the indices between the two
                throw new NotImplementedException();
            }
        }

        public void SetIndices(int toOffset, int fromOffset, int length)
        {
            int[] fromIndices = new int[length];
            if (indexBuffer.IndexCount > fromOffset)
            {
                indexBuffer.GetData(4 * fromOffset, fromIndices, 0, Math.Min(indexBuffer.IndexCount - fromOffset, length));
            }
            for (int i = Math.Max(indexBuffer.IndexCount - fromOffset, 0); i < length; i++)
            {
                fromIndices[i] = pendingIndices[i + fromOffset - indexBuffer.IndexCount];
            }
            SetIndices(toOffset, fromIndices);
        }

        // TODO: actually shrink
        internal void ReduceIndices(int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (pendingIndices.Count > 0)
                {
                    pendingIndices.RemoveAt(pendingIndices.Count - 1);
                }
                else
                {
                    indexCount--;
                }
            }
        }
    }
}

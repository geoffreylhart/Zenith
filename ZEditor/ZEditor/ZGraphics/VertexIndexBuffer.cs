using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZGraphics
{
    class VertexIndexBuffer : IDisposable
    {
        public VertexBuffer vertexBuffer;
        public IndexBuffer indexBuffer;

        public VertexIndexBuffer(VertexBuffer vertexBuffer, IndexBuffer indexBuffer)
        {
            this.vertexBuffer = vertexBuffer;
            this.indexBuffer = indexBuffer;
        }

        public void Dispose()
        {
            if (vertexBuffer != null) vertexBuffer.Dispose();
            if (indexBuffer != null) indexBuffer.Dispose();
        }
    }
}

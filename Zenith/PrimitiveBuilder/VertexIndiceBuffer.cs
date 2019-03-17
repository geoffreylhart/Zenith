using System;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.PrimitiveBuilder
{
    public class VertexIndiceBuffer : IDisposable
    {
        public VertexBuffer vertices;
        public IndexBuffer indices;
        public Texture2D texture;

        public void Dispose()
        {
            if (vertices != null) vertices.Dispose();
            if (indices != null) indices.Dispose();
            if (texture != null) texture.Dispose();
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.PrimitiveBuilder
{
    internal class CubeBuilder
    {
        // according to Blender front=-y, back=y, left=-x, right=x, up=z, down=-z
        internal static VertexIndiceBuffer MakeBasicCube(GraphicsDevice graphicsDevice)
        {
            VertexIndiceBuffer buffer = new VertexIndiceBuffer();

            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                Vector3 position = new Vector3(i < 4 ? -1 : 1, i % 4 < 2 ? -1 : 1, i % 2 < 1 ? -1 : 1);
                Vector3 normal = new Vector3(true?-1:1, true ? -1 : 1, true ? -1 : 1);
                Vector2 tex = new Vector2(0, 0); // don't care
                //vertices.Add(new VertexPositionColor(position, Color.Green));
                vertices.Add(new VertexPositionNormalTexture(position, position, tex));
            }
            // front face, triangles diagonal goes from bottom left to top right, triangles are formed by going clockwise to make them visible
            // all the other squares triangles share rotational symmetry (sort of) with this first one (basically copy-paste the first square and then rotate it around the origin purely with the x-axis or z-axis)
            indices.Add(0);
            indices.Add(1);
            indices.Add(5);
            indices.Add(0);
            indices.Add(5);
            indices.Add(4);
            // back face
            indices.Add(3);
            indices.Add(2);
            indices.Add(6);
            indices.Add(3);
            indices.Add(6);
            indices.Add(7);
            // right face
            indices.Add(4);
            indices.Add(5);
            indices.Add(7);
            indices.Add(4);
            indices.Add(7);
            indices.Add(6);
            // left face
            indices.Add(2);
            indices.Add(3);
            indices.Add(1);
            indices.Add(2);
            indices.Add(1);
            indices.Add(0);
            // top face
            indices.Add(1);
            indices.Add(3);
            indices.Add(7);
            indices.Add(1);
            indices.Add(7);
            indices.Add(5);
            // bottom face
            indices.Add(2);
            indices.Add(0);
            indices.Add(4);
            indices.Add(2);
            indices.Add(4);
            indices.Add(6);
            // TODO: change back to BufferUsage.WriteOnly if applicable
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.None);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            buffer.indices.SetData(indices.ToArray());
            return buffer;
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.PrimitiveBuilder
{
    public class CubeBuilder
    {
        // according to Blender front=-y, back=y, left=-x, right=x, up=z, down=-z
        internal static VertexIndiceBuffer MakeBasicCube(GraphicsDevice graphicsDevice)
        {
            return MakeBasicCube(graphicsDevice, new Vector3(-1, 1, -1), Vector3.UnitX * 2, -Vector3.UnitY * 2, Vector3.UnitZ * 2); // why does this bug out if we use * 1??
        }

        internal static VertexIndiceBuffer MakeBasicCube(GraphicsDevice graphicsDevice, Vector3 corner, Vector3 offx, Vector3 offy, Vector3 offz)
        {
            VertexIndiceBuffer buffer = new VertexIndiceBuffer();

            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                Vector3 position = corner;
                if (i < 4) position += offx;
                if (i % 4 < 2) position += offy;
                if (i % 2 < 1) position += offz;
                Vector3 normal = new Vector3(true ? -1 : 1, true ? -1 : 1, true ? -1 : 1);
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

        internal static VertexIndiceBuffer MakeBasicBuildingCube(GraphicsDevice graphicsDevice, double lat, double lon)
        {
            double radius = 1;
            double dy = Math.Sin(lat);
            double dxz = Math.Cos(lat);
            double dx = Math.Cos(lon) * dxz;
            double dz = Math.Sin(lon) * dxz;

            // stole this equation
            Vector3 normal = new Vector3((float)dx, (float)dz, (float)dy); // forgot to switch dy and dz here too
            Vector3 corner = new Vector3((float)(dx * radius), (float)(dz * radius), (float)(dy * radius));
            return MakeBasicCube(graphicsDevice, corner, Vector3.UnitX, -Vector3.UnitY, Vector3.UnitZ);
        }
    }
}

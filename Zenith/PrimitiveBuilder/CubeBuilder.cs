using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.MathHelpers;

namespace Zenith.PrimitiveBuilder
{
    public class CubeBuilder
    {
        // according to Blender front=-y, back=y, left=-x, right=x, up=z, down=-z
        internal static VertexIndiceBuffer MakeBasicCube(GraphicsDevice graphicsDevice)
        {
            return MakeBasicCube(graphicsDevice, new Vector3(-1, -1, -1), Vector3.UnitX * 2, Vector3.UnitY * 2, Vector3.UnitZ * 2); // why does this bug out if we use * 1??
        }

        internal static VertexIndiceBuffer MakeBasicCube(GraphicsDevice graphicsDevice, Vector3 corner, Vector3 offx, Vector3 offy, Vector3 offz)
        {
            VertexIndiceBuffer buffer = new VertexIndiceBuffer();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();
            Vector3 corner2 = corner + offx + offy + offz;
            AddQuad(vertices, indices, corner, offz, offx); // front
            AddQuad(vertices, indices, corner, offy, offz); // left
            AddQuad(vertices, indices, corner, offx, offy); // bottom
            // clearly reverse the direction and rotationness to do the opposite corner
            AddQuad(vertices, indices, corner2, -offx, -offz); // back
            AddQuad(vertices, indices, corner2, -offz, -offy); // right
            AddQuad(vertices, indices, corner2, -offy, -offx); // top
            // TODO: change back to BufferUsage.WriteOnly if applicable
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.None);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            buffer.indices.SetData(indices.ToArray());
            return buffer;
        }

        // unit1 must go clockwise to reach unit2 to make the quad visible
        private static void AddQuad(List<VertexPositionNormalTexture> vertices, List<int> indices, Vector3 corner, Vector3 unit1, Vector3 unit2)
        {
            Vector2 tex = new Vector2(0, 0); // don't care
            Vector3 normal = Vector3.Cross(unit2, unit1);
            normal.Normalize();
            indices.Add(vertices.Count);
            indices.Add(vertices.Count + 1);
            indices.Add(vertices.Count + 3);
            indices.Add(vertices.Count);
            indices.Add(vertices.Count + 3);
            indices.Add(vertices.Count + 2);
            vertices.Add(new VertexPositionNormalTexture(corner, normal, tex));
            vertices.Add(new VertexPositionNormalTexture(corner + unit1, normal, tex));
            vertices.Add(new VertexPositionNormalTexture(corner + unit2, normal, tex));
            vertices.Add(new VertexPositionNormalTexture(corner + unit1 + unit2, normal, tex));
        }

        internal static VertexIndiceBuffer MakeBasicBuildingCube(GraphicsDevice graphicsDevice, Vector3 corner)
        {
            float size = 0.00000478388328855618575244743469043f; // 100f if the radius of earth is 1
            //size = 0.001f;
            Vector3 up = corner;
            up.Normalize(); // this heavily assumes the corner is on the surface of the globe
            Vector3 right = Vector3.Cross(Vector3.UnitZ, up); // cross product points towards you if a->b is counter-clockwise
            right.Normalize();
            Vector3 back = Vector3.Cross(up, right);
            back.Normalize();
            corner -= right * size / 2;
            corner -= back * size / 2;
            return MakeBasicCube(graphicsDevice, corner, right * size, back * size, up * size);
        }
    }
}

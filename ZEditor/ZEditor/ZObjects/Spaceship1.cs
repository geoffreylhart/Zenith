using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZGraphics;

namespace ZEditor.ZObjects
{
    public class Spaceship1
    {
        public static VertexIndexBuffer MakeShip(GraphicsDevice graphicsDevice)
        {
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();
            AddUnitCube(vertices, indices);
            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());
            return new VertexIndexBuffer(vertexBuffer, indexBuffer);
        }

        private static void AddUnitCube(List<VertexPositionNormalTexture> vertices, List<int> indices)
        {
            // according to vector3, 1,1,1 is right, up, backward
            // atm we will construct the cube assuming this is from a spaceship perspective
            // this means that the 111, would be the lefttopback from outside perspective
            var rbf = new Vector3(0, 0, 0);
            var rbb = new Vector3(0, 0, 1);
            var rtf = new Vector3(0, 1, 0);
            var rtb = new Vector3(0, 1, 1);
            var lbf = new Vector3(1, 0, 0);
            var lbb = new Vector3(1, 0, 1);
            var ltf = new Vector3(1, 1, 0);
            var ltb = new Vector3(1, 1, 1);
            // top, bottom, left (from outside perspective), right, front, back
            AddQuad(vertices, indices, ltb, rtb, rtf, ltf); // lefttopback, righttopback, righttopfront, lefttopfront
            AddQuad(vertices, indices, lbf, rbf, rbb, lbb); // leftbottomfront, rightbottomfront, rightbottomback, leftbottomback
            AddQuad(vertices, indices, ltb, ltf, lbf, lbb); // lefttopback, lefttopfront, leftbottomfront, leftbottomback
            AddQuad(vertices, indices, rtf, rtb, rbb, rbf); // righttopfront, righttopback, rightbottomback, rightbottomfront
            AddQuad(vertices, indices, ltf, rtf, rbf, lbf); // lefttopfront, righttopfront, rightbottomfront, leftbottomfront
            AddQuad(vertices, indices, rtb, ltb, lbb, rbb); // righttopback, lefttopback, leftbottomback, rightbottomback
        }

        private static void AddQuad(List<VertexPositionNormalTexture> vertices, List<int> indices, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft)
        {
            // preferred quad order topleft, topright, bottomright, topleft, bottomright, bottomleft
            indices.Add(vertices.Count);
            indices.Add(vertices.Count + 1);
            indices.Add(vertices.Count + 2);
            indices.Add(vertices.Count);
            indices.Add(vertices.Count + 2);
            indices.Add(vertices.Count + 3);
            Vector3 normal = Vector3.Cross(topRight - topLeft, bottomLeft - topLeft);
            normal.Normalize();
            vertices.Add(new VertexPositionNormalTexture(topLeft, normal, new Vector2(0, 0)));
            vertices.Add(new VertexPositionNormalTexture(topRight, normal, new Vector2(1, 0)));
            vertices.Add(new VertexPositionNormalTexture(bottomRight, normal, new Vector2(1, 1)));
            vertices.Add(new VertexPositionNormalTexture(bottomLeft, normal, new Vector2(0, 1)));
        }
    }
}

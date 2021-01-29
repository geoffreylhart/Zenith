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

            AddHull(vertices, indices);
            //AddUnitCube(vertices, indices);

            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());
            return new VertexIndexBuffer(vertexBuffer, indexBuffer);
        }

        private static void AddHull(List<VertexPositionNormalTexture> vertices, List<int> indices)
        {
            var topBackLeft = new Vector3(1, 2, 4);
            var topBackRight = new Vector3(-1, 2, 4);
            var topFrontLeft = new Vector3(1, 2, 2);
            var topFrontRight = new Vector3(-1, 2, 2);
            var bottomBackLeft = new Vector3(2, 0, 4);
            var bottomBackRight = new Vector3(-2, 0, 4);
            var bottomFrontLeft = new Vector3(2, 0, 0);
            var bottomFrontRight = new Vector3(-2, 0, 0);

            var leftDoorBottomBackLeft = new Vector3(1, 0, 3.5f);
            var leftDoorBottomBackRight = leftDoorBottomBackLeft + Vector3.Forward;
            var leftDoorBottomFrontLeft = new Vector3(2, 0, 3.5f);
            var leftDoorBottomFrontRight = leftDoorBottomFrontLeft + Vector3.Forward;
            var leftDoorTopBackLeft = new Vector3(1, 1.5f, 3.5f);
            var leftDoorTopBackRight = leftDoorTopBackLeft + Vector3.Forward;
            var leftDoorTopFrontLeft = new Vector3(1.25f, 2, 3.5f);
            var leftDoorTopFrontRight = leftDoorTopFrontLeft + Vector3.Forward;

            var rightDoorBottomBackLeft = leftDoorBottomBackRight * new Vector3(-1, 1, 1);
            var rightDoorBottomBackRight = leftDoorBottomBackLeft * new Vector3(-1, 1, 1);
            var rightDoorBottomFrontLeft = leftDoorBottomFrontRight * new Vector3(-1, 1, 1);
            var rightDoorBottomFrontRight = leftDoorBottomFrontLeft * new Vector3(-1, 1, 1);
            var rightDoorTopBackLeft = leftDoorTopBackRight * new Vector3(-1, 1, 1);
            var rightDoorTopBackRight = leftDoorTopBackLeft * new Vector3(-1, 1, 1);
            var rightDoorTopFrontLeft = leftDoorTopFrontRight * new Vector3(-1, 1, 1);
            var rightDoorTopFrontRight = leftDoorTopFrontLeft * new Vector3(-1, 1, 1);

            var bottomBackMidLeft = new Vector3(1, 0, 4);
            var bottomBackMidRight = new Vector3(-1, 0, 4);
            var bottomFrontMidLeft = new Vector3(1, 0, 0);
            var bottomFrontMidRight = new Vector3(-1, 0, 0);
            // top, bottom, left, right, front, back
            AddQuad(vertices, indices, topBackLeft, topBackRight, topFrontRight, topFrontLeft);
            // bottom
            AddQuad(vertices, indices, bottomFrontLeft, bottomFrontMidLeft, leftDoorBottomBackRight, leftDoorBottomFrontRight);
            AddQuad(vertices, indices, bottomFrontMidRight, bottomFrontRight, rightDoorBottomFrontLeft, rightDoorBottomBackLeft);
            AddQuad(vertices, indices, leftDoorBottomFrontLeft, leftDoorBottomBackLeft, bottomBackMidLeft, bottomBackLeft);
            AddQuad(vertices, indices, rightDoorBottomBackRight, rightDoorBottomFrontRight, bottomBackRight, bottomBackMidRight);
            // left
            AddQuad(vertices, indices, topBackLeft, topFrontLeft, bottomFrontLeft, bottomBackLeft);
            // right
            AddQuad(vertices, indices, topFrontRight, topBackRight, bottomBackRight, bottomFrontRight);
            // front
            AddQuad(vertices, indices, topFrontLeft, topFrontRight, bottomFrontRight, bottomFrontLeft);
            // back
            AddQuad(vertices, indices, topBackRight, topBackLeft, bottomBackLeft, bottomBackRight);

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
            Vector3 normal = Vector3.Cross(bottomLeft - topLeft, topRight - topLeft);
            normal.Normalize();
            vertices.Add(new VertexPositionNormalTexture(topLeft, normal, new Vector2(0, 0)));
            vertices.Add(new VertexPositionNormalTexture(topRight, normal, new Vector2(1, 0)));
            vertices.Add(new VertexPositionNormalTexture(bottomRight, normal, new Vector2(1, 1)));
            vertices.Add(new VertexPositionNormalTexture(bottomLeft, normal, new Vector2(0, 1)));
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates
{
    class Mesh : ITemplate
    {
        List<Vector3> positions = new List<Vector3>();
        List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
        List<int> indices = new List<int>();
        VertexIndexBuffer buffer;

        public void Load(StreamReader reader)
        {
            var currLine = reader.ReadLine();
            if (!currLine.Contains("Vertices")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                positions.Add(new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2])));
                currLine = reader.ReadLine();
            }
            currLine = reader.ReadLine();
            if (!currLine.Contains("Quads")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                Vector3 topLeft = positions[int.Parse(split[0])];
                Vector3 topRight = positions[int.Parse(split[1])];
                Vector3 bottomRight = positions[int.Parse(split[2])];
                Vector3 bottomLeft = positions[int.Parse(split[3])];
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
                currLine = reader.ReadLine();
            }
            currLine = reader.ReadLine();
            if (!currLine.Contains("Tris")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                Vector3 v1 = positions[int.Parse(split[0])];
                Vector3 v2 = positions[int.Parse(split[1])];
                Vector3 v3 = positions[int.Parse(split[2])];
                // preferred quad order topleft, topright, bottomright, topleft, bottomright, bottomleft
                indices.Add(vertices.Count);
                indices.Add(vertices.Count + 1);
                indices.Add(vertices.Count + 2);
                Vector3 normal = Vector3.Cross(v3 - v1, v2 - v1);
                normal.Normalize();
                vertices.Add(new VertexPositionNormalTexture(v1, normal, new Vector2(0, 0)));
                vertices.Add(new VertexPositionNormalTexture(v2, normal, new Vector2(1, 0)));
                vertices.Add(new VertexPositionNormalTexture(v3, normal, new Vector2(1, 1)));
                currLine = reader.ReadLine();
            }
        }

        public void Save(StreamWriter writer)
        {
            throw new NotImplementedException();
        }

        public VertexIndexBuffer MakeBuffer(GraphicsDevice graphicsDevice)
        {
            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());
            return new VertexIndexBuffer(vertexBuffer, indexBuffer);
        }
    }
}

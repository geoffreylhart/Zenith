using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZControl;
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
        PointCollectionTracker tracker = new PointCollectionTracker();

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
                tracker.Track(vertices.Count - 4, topLeft);
                tracker.Track(vertices.Count - 3, topRight);
                tracker.Track(vertices.Count - 2, bottomRight);
                tracker.Track(vertices.Count - 1, bottomLeft);
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
                tracker.Track(vertices.Count - 3, v1);
                tracker.Track(vertices.Count - 2, v2);
                tracker.Track(vertices.Count - 1, v3);
                currLine = reader.ReadLine();
            }
        }

        public void Save(StreamWriter writer)
        {
            throw new NotImplementedException();
        }

        public VertexIndexBuffer MakeBuffer(GraphicsDevice graphicsDevice)
        {
            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.None);
            vertexBuffer.SetData(vertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            indexBuffer.SetData(indices.ToArray());
            buffer = new VertexIndexBuffer(vertexBuffer, indexBuffer);
            return buffer;
        }

        public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, FPSCamera camera, GraphicsDevice graphicsDevice)
        {
            if (buffer != null)
            {
                int nearestIndice = tracker.GetNearest(camera.GetPosition(), camera.GetLookUnitVector(mouseState.X, mouseState.Y, graphicsDevice));
                VertexPositionNormalTexture[] temp = new VertexPositionNormalTexture[1];
                buffer.vertexBuffer.GetData<VertexPositionNormalTexture>(VertexPositionNormalTexture.VertexDeclaration.VertexStride * nearestIndice, temp, 0, 1);
                temp[0].Position = temp[0].Position + Vector3.Forward * 0.01f;
                tracker.Update(nearestIndice, temp[0].Position);
                buffer.vertexBuffer.SetData<VertexPositionNormalTexture>(VertexPositionNormalTexture.VertexDeclaration.VertexStride * nearestIndice, temp, 0, 1, VertexPositionNormalTexture.VertexDeclaration.VertexStride);
            }
        }
    }
}

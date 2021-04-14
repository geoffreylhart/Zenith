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
        public class PositionInfo
        {
            public Vector3 v;
            public List<VertexInfo> vertices = new List<VertexInfo>();


            public PositionInfo(float x, float y, float z)
            {
                this.v = new Vector3(x, y, z);
            }
        }

        public class VertexInfo
        {
            public int index;
            public int polygonStartIndex;
            public int polygonNumVertices;

            public VertexInfo(int index, int polygonStartIndex, int polygonNumVertices)
            {
                this.index = index;
                this.polygonStartIndex = polygonStartIndex;
                this.polygonNumVertices = polygonNumVertices;
            }
        }

        List<PositionInfo> positions = new List<PositionInfo>();
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
                positions.Add(new PositionInfo(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2])));
                tracker.Track(positions.Count - 1, positions[positions.Count - 1].v);
                currLine = reader.ReadLine();
            }
            currLine = reader.ReadLine();
            if (!currLine.Contains("Quads")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                PositionInfo topLeft = positions[int.Parse(split[0])];
                PositionInfo topRight = positions[int.Parse(split[1])];
                PositionInfo bottomRight = positions[int.Parse(split[2])];
                PositionInfo bottomLeft = positions[int.Parse(split[3])];
                // preferred quad order topleft, topright, bottomright, topleft, bottomright, bottomleft
                indices.Add(vertices.Count);
                indices.Add(vertices.Count + 1);
                indices.Add(vertices.Count + 2);
                indices.Add(vertices.Count);
                indices.Add(vertices.Count + 2);
                indices.Add(vertices.Count + 3);
                Vector3 normal = CalculateNormal(topLeft.v, topRight.v, bottomLeft.v);
                vertices.Add(new VertexPositionNormalTexture(topLeft.v, normal, new Vector2(0, 0)));
                vertices.Add(new VertexPositionNormalTexture(topRight.v, normal, new Vector2(1, 0)));
                vertices.Add(new VertexPositionNormalTexture(bottomRight.v, normal, new Vector2(1, 1)));
                vertices.Add(new VertexPositionNormalTexture(bottomLeft.v, normal, new Vector2(0, 1)));
                topLeft.vertices.Add(new VertexInfo(vertices.Count - 4, vertices.Count - 4, 4));
                topRight.vertices.Add(new VertexInfo(vertices.Count - 3, vertices.Count - 4, 4));
                bottomRight.vertices.Add(new VertexInfo(vertices.Count - 2, vertices.Count - 4, 4));
                bottomLeft.vertices.Add(new VertexInfo(vertices.Count - 1, vertices.Count - 4, 4));
                currLine = reader.ReadLine();
            }
            currLine = reader.ReadLine();
            if (!currLine.Contains("Tris")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                PositionInfo v1 = positions[int.Parse(split[0])];
                PositionInfo v2 = positions[int.Parse(split[1])];
                PositionInfo v3 = positions[int.Parse(split[2])];
                // preferred quad order topleft, topright, bottomright, topleft, bottomright, bottomleft
                indices.Add(vertices.Count);
                indices.Add(vertices.Count + 1);
                indices.Add(vertices.Count + 2);
                Vector3 normal = CalculateNormal(v1.v, v2.v, v3.v);
                vertices.Add(new VertexPositionNormalTexture(v1.v, normal, new Vector2(0, 0)));
                vertices.Add(new VertexPositionNormalTexture(v2.v, normal, new Vector2(1, 0)));
                vertices.Add(new VertexPositionNormalTexture(v3.v, normal, new Vector2(1, 1)));
                v1.vertices.Add(new VertexInfo(vertices.Count - 3, vertices.Count - 3, 3));
                v2.vertices.Add(new VertexInfo(vertices.Count - 2, vertices.Count - 3, 3));
                v3.vertices.Add(new VertexInfo(vertices.Count - 1, vertices.Count - 3, 3));
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

        int? draggingIndex = null;
        // note: getting too confusing, since we don't split quads currently into 2 detached triangles, we can't update quads with 2 different normals...
        public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, FPSCamera camera, GraphicsDevice graphicsDevice)
        {
            if (buffer != null)
            {
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (draggingIndex == null)
                    {
                        draggingIndex = tracker.GetNearest(camera.GetPosition(), camera.GetLookUnitVector(mouseState.X, mouseState.Y, graphicsDevice));
                    }
                }
                else
                {
                    draggingIndex = null;
                }
                if (draggingIndex != null)
                {
                    VertexPositionNormalTexture[] temp = new VertexPositionNormalTexture[1];
                    buffer.vertexBuffer.GetData<VertexPositionNormalTexture>(VertexPositionNormalTexture.VertexDeclaration.VertexStride * positions[draggingIndex.Value].vertices[0].index, temp, 0, 1);
                    float oldDistance = (temp[0].Position - camera.GetPosition()).Length();
                    temp[0].Position = camera.GetPosition() + camera.GetLookUnitVector(mouseState.X, mouseState.Y, graphicsDevice) * oldDistance;
                    // snap to grid
                    temp[0].Position.X = (float)Math.Round(temp[0].Position.X * 4) / 4;
                    temp[0].Position.Y = (float)Math.Round(temp[0].Position.Y * 4) / 4;
                    temp[0].Position.Z = (float)Math.Round(temp[0].Position.Z * 4) / 4;
                    tracker.Update(draggingIndex.Value, temp[0].Position);
                    foreach (var vertex in positions[draggingIndex.Value].vertices)
                    {
                        VertexPositionNormalTexture temp2 = vertices[vertex.index];
                        temp2.Position = temp[0].Position;
                        vertices[vertex.index] = temp2;
                        Vector3 newNormal;
                        if (vertex.polygonNumVertices == 3)
                        {
                            newNormal = CalculateNormal(vertices[vertex.polygonStartIndex].Position, vertices[vertex.polygonStartIndex + 1].Position, vertices[vertex.polygonStartIndex + 2].Position);
                        }
                        else
                        {
                            newNormal = CalculateNormal(vertices[vertex.polygonStartIndex].Position, vertices[vertex.polygonStartIndex + 1].Position, vertices[vertex.polygonStartIndex + 2].Position, vertices[vertex.polygonStartIndex + 3].Position);
                        }
                        temp[0].Normal = newNormal;
                        for (int i = 0; i < vertex.polygonNumVertices; i++)
                        {
                            VertexPositionNormalTexture[] temp3 = new VertexPositionNormalTexture[1];
                            buffer.vertexBuffer.GetData<VertexPositionNormalTexture>(VertexPositionNormalTexture.VertexDeclaration.VertexStride * (vertex.polygonStartIndex + i), temp3, 0, 1);
                            temp3[0].Normal = newNormal;
                            buffer.vertexBuffer.SetData<VertexPositionNormalTexture>(VertexPositionNormalTexture.VertexDeclaration.VertexStride * (vertex.polygonStartIndex + i), temp3, 0, 1, VertexPositionNormalTexture.VertexDeclaration.VertexStride);
                        }
                        buffer.vertexBuffer.SetData<VertexPositionNormalTexture>(VertexPositionNormalTexture.VertexDeclaration.VertexStride * vertex.index, temp, 0, 1, VertexPositionNormalTexture.VertexDeclaration.VertexStride);
                    }
                }
            }
        }

        private Vector3 CalculateNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 normal = Vector3.Cross(v3 - v1, v2 - v1);
            normal.Normalize();
            return normal;
        }

        private Vector3 CalculateNormal(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            Vector3 normal = CalculateNormal(v1, v2, v3) + CalculateNormal(v1, v2, v4) + CalculateNormal(v1, v3, v4) + CalculateNormal(v2, v3, v4);
            normal.Normalize();
            return normal;
        }
    }
}

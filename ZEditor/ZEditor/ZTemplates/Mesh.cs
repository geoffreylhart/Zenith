using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates
{
    // feature todo list:
    // rewrite to make better sense
    // matching basic blender functionality:
    // grey shader thats vaguely specular
    // objects that are selectable based on mesh and sortable (appears to not care about time but always selects something different, goes through full stack if you never move mouse)
    // edit mode which renders primarily selected points/edges as whiter, selected as oranger, and fades from orangish in point selection mode
    //  middle mouse click to drag around origin
    // ctrl-click for snap, shift click for fine control
    // x,y,z to lock to those or shift-x-y-z (cancel by typing again) with i guess grids
    // display point vertices
    // shift click to select multiple, ctrl click to trace and select multiple
    // ctrl-r for loop cuts (highlight with yellow) scroll wheel to increase count
    // e extrude
    // s to scale
    // f to make face
    // z wireframe
    // a select all
    // grid base at y=0? which extends infinite and is thicker every 10
    // rgb xyz axis and widget
    // selecting mesh gives outline and fainter outline if certain depth in
    // undo/redo
    class Mesh : ITemplate
    {
        public class PositionInfo
        {
            public Vector3 v;
            public List<VertexInfo> vertices = new List<VertexInfo>();
            public List<int> lineIndices = new List<int>();
            public List<int> pointIndices = new List<int>();


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
        List<VertexPositionNormalTexture> faceVertices = new List<VertexPositionNormalTexture>();
        List<int> faceIndices = new List<int>();
        List<VertexPositionColor> lineVertices = new List<VertexPositionColor>();
        List<int> lineIndices = new List<int>();
        List<VertexPositionColorTexture> pointVertices = new List<VertexPositionColorTexture>();
        List<int> pointIndices = new List<int>();
        VertexIndexBuffer faceBuffer;
        VertexIndexBuffer lineBuffer;
        VertexIndexBuffer pointBuffer;
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
                AddPoly(currLine, 4);
                currLine = reader.ReadLine();
            }
            currLine = reader.ReadLine();
            if (!currLine.Contains("Tris")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                AddPoly(currLine, 3);
                currLine = reader.ReadLine();
            }
        }

        private void AddPoly(string currLine, int sides)
        {
            // TODO: remove duplicate lines/points
            var split = currLine.Trim().Split(',');
            var infos = split.Select(x => positions[int.Parse(x)]).ToArray();
            for (int i = 0; i < sides - 2; i++)
            {
                faceIndices.Add(faceVertices.Count);
                faceIndices.Add(faceVertices.Count + 1 + i);
                faceIndices.Add(faceVertices.Count + 2 + i);
            }
            for (int i = 0; i < sides; i++)
            {
                lineIndices.Add(lineVertices.Count + i);
                lineIndices.Add(lineVertices.Count + (i + 1) % sides);
                pointIndices.Add(pointVertices.Count + i * 4);
                pointIndices.Add(pointVertices.Count + 1 + i * 4);
                pointIndices.Add(pointVertices.Count + 2 + i * 4);
                pointIndices.Add(pointVertices.Count + i * 4);
                pointIndices.Add(pointVertices.Count + 2 + i * 4);
                pointIndices.Add(pointVertices.Count + 3 + i * 4);
            }
            Vector3 normal = CalculateNormal(infos.Select(x => x.v).ToArray());
            for (int i = 0; i < sides; i++)
            {
                // TODO: texture coordinate
                faceVertices.Add(new VertexPositionNormalTexture(infos[i].v, normal, new Vector2(0, 0)));
                lineVertices.Add(new VertexPositionColor(infos[i].v, Color.Black));
                pointVertices.Add(new VertexPositionColorTexture(infos[i].v, Color.Black, new Vector2(0, 0)));
                pointVertices.Add(new VertexPositionColorTexture(infos[i].v, Color.Black, new Vector2(1, 0)));
                pointVertices.Add(new VertexPositionColorTexture(infos[i].v, Color.Black, new Vector2(1, 1)));
                pointVertices.Add(new VertexPositionColorTexture(infos[i].v, Color.Black, new Vector2(0, 1)));
            }
            for (int i = 0; i < sides; i++)
            {
                infos[i].vertices.Add(new VertexInfo(faceVertices.Count - sides + i, faceVertices.Count - sides, sides));
                infos[i].lineIndices.Add(lineVertices.Count - sides + i);
                infos[i].pointIndices.Add(pointVertices.Count - sides * 4 + i * 4);
            }
        }

        public void Save(StreamWriter writer)
        {
            throw new NotImplementedException();
        }

        public VertexIndexBuffer MakeFaceBuffer(GraphicsDevice graphicsDevice)
        {
            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, faceVertices.Count, BufferUsage.None);
            vertexBuffer.SetData(faceVertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, faceIndices.Count, BufferUsage.None);
            indexBuffer.SetData(faceIndices.ToArray());
            faceBuffer = new VertexIndexBuffer(vertexBuffer, indexBuffer);
            return faceBuffer;
        }

        public VertexIndexBuffer MakeLineBuffer(GraphicsDevice graphicsDevice)
        {
            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, lineVertices.Count, BufferUsage.None);
            vertexBuffer.SetData(lineVertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, lineIndices.Count, BufferUsage.None);
            indexBuffer.SetData(lineIndices.ToArray());
            lineBuffer = new VertexIndexBuffer(vertexBuffer, indexBuffer);
            return lineBuffer;
        }

        public VertexIndexBuffer MakePointBuffer(GraphicsDevice graphicsDevice)
        {
            var vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColorTexture.VertexDeclaration, pointVertices.Count, BufferUsage.None);
            vertexBuffer.SetData(pointVertices.ToArray());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, pointIndices.Count, BufferUsage.None);
            indexBuffer.SetData(pointIndices.ToArray());
            pointBuffer = new VertexIndexBuffer(vertexBuffer, indexBuffer);
            return pointBuffer;
        }

        int? draggingIndex = null;
        // note: getting too confusing, since we don't split quads currently into 2 detached triangles, we can't update quads with 2 different normals...
        public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, FPSCamera camera, GraphicsDevice graphicsDevice)
        {
            if (faceBuffer != null)
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
                    // get position and update tracker
                    Vector3 newPosition = positions[draggingIndex.Value].v;
                    float oldDistance = (newPosition - camera.GetPosition()).Length();
                    newPosition = camera.GetPosition() + camera.GetLookUnitVector(mouseState.X, mouseState.Y, graphicsDevice) * oldDistance;
                    newPosition.X = (float)Math.Round(newPosition.X * 4) / 4;
                    newPosition.Y = (float)Math.Round(newPosition.Y * 4) / 4;
                    newPosition.Z = (float)Math.Round(newPosition.Z * 4) / 4;
                    tracker.Update(draggingIndex.Value, newPosition);
                    // update faces
                    VertexPositionNormalTexture[] temp = new VertexPositionNormalTexture[1];
                    temp[0].Position = newPosition;
                    foreach (var vertex in positions[draggingIndex.Value].vertices)
                    {
                        VertexPositionNormalTexture temp2 = faceVertices[vertex.index];
                        temp2.Position = temp[0].Position;
                        faceVertices[vertex.index] = temp2;
                        Vector3 newNormal;
                        if (vertex.polygonNumVertices == 3)
                        {
                            newNormal = CalculateNormal(faceVertices[vertex.polygonStartIndex].Position, faceVertices[vertex.polygonStartIndex + 1].Position, faceVertices[vertex.polygonStartIndex + 2].Position);
                        }
                        else
                        {
                            newNormal = CalculateNormal(faceVertices[vertex.polygonStartIndex].Position, faceVertices[vertex.polygonStartIndex + 1].Position, faceVertices[vertex.polygonStartIndex + 2].Position, faceVertices[vertex.polygonStartIndex + 3].Position);
                        }
                        temp[0].Normal = newNormal;
                        for (int i = 0; i < vertex.polygonNumVertices; i++)
                        {
                            VertexPositionNormalTexture[] temp3 = new VertexPositionNormalTexture[1];
                            faceBuffer.vertexBuffer.GetData(VertexPositionNormalTexture.VertexDeclaration.VertexStride * (vertex.polygonStartIndex + i), temp3, 0, 1);
                            temp3[0].Normal = newNormal;
                            faceBuffer.vertexBuffer.SetData(VertexPositionNormalTexture.VertexDeclaration.VertexStride * (vertex.polygonStartIndex + i), temp3, 0, 1, VertexPositionNormalTexture.VertexDeclaration.VertexStride);
                        }
                        faceBuffer.vertexBuffer.SetData(VertexPositionNormalTexture.VertexDeclaration.VertexStride * vertex.index, temp, 0, 1, VertexPositionNormalTexture.VertexDeclaration.VertexStride);
                    }
                    // update lines
                    var temp4 = new VertexPositionColor[] { new VertexPositionColor(newPosition, Color.Black) };
                    foreach (var index in positions[draggingIndex.Value].lineIndices)
                    {
                        lineBuffer.vertexBuffer.SetData(VertexPositionColor.VertexDeclaration.VertexStride * index, temp4, 0, 1, VertexPositionColor.VertexDeclaration.VertexStride);
                    }
                    // update points
                    var temp5 = new VertexPositionColorTexture[4];
                    temp5[0] = new VertexPositionColorTexture(newPosition, Color.Black, new Vector2(0, 0));
                    temp5[1] = new VertexPositionColorTexture(newPosition, Color.Black, new Vector2(1, 0));
                    temp5[2] = new VertexPositionColorTexture(newPosition, Color.Black, new Vector2(1, 1));
                    temp5[3] = new VertexPositionColorTexture(newPosition, Color.Black, new Vector2(0, 1));
                    foreach (var index in positions[draggingIndex.Value].pointIndices)
                    {
                        pointBuffer.vertexBuffer.SetData(VertexPositionColorTexture.VertexDeclaration.VertexStride * index, temp5, 0, 4, VertexPositionColorTexture.VertexDeclaration.VertexStride);
                    }
                }
            }
        }

        private Vector3 CalculateNormal(params Vector3[] vectors)
        {
            Vector3 normal = Vector3.Zero;
            // TODO: non-random averaging
            for (int i = 0; i < vectors.Length - 2; i++)
            {
                normal += Vector3.Cross(vectors[i + 2] - vectors[i], vectors[i + 1] - vectors[i + i]);
            }
            normal.Normalize();
            return normal;
        }
    }
}

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
using ZEditor.ZTemplates.Mesh;

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
    class MeshTemplate : ITemplate
    {
        FaceMesh faceMesh = new FaceMesh();
        LineMesh lineMesh = new LineMesh();
        PointMesh pointMesh = new PointMesh();
        Vector3[] positions;
        PointCollectionTracker tracker = new PointCollectionTracker();

        public void Load(StreamReader reader)
        {
            var currLine = reader.ReadLine();
            if (!currLine.Contains("Vertices")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            var positionList = new List<Vector3>();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                positionList.Add(new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2])));
                tracker.Track(positionList.Count - 1, positionList[positionList.Count - 1]);
                currLine = reader.ReadLine();
            }
            positions = positionList.ToArray();
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
            var split = currLine.Trim().Split(',').Select(x => int.Parse(x)).ToArray();
            faceMesh.AddItem(split);
            for (int i = 0; i < sides; i++)
            {
                lineMesh.AddItem(new int[] { split[i], split[(i + 1) % sides] });
                pointMesh.AddItem(new int[] { split[i] });
            }
        }

        public void Save(StreamWriter writer)
        {
            throw new NotImplementedException();
        }

        public VertexIndexBuffer MakeFaceBuffer(GraphicsDevice graphicsDevice)
        {
            return faceMesh.MakeBuffer(positions, graphicsDevice);
        }

        public VertexIndexBuffer MakeLineBuffer(GraphicsDevice graphicsDevice)
        {
            return lineMesh.MakeBuffer(positions, graphicsDevice);
        }

        public VertexIndexBuffer MakePointBuffer(GraphicsDevice graphicsDevice)
        {
            return pointMesh.MakeBuffer(positions, graphicsDevice);
        }

        int? draggingIndex = null;
        // note: getting too confusing, since we don't split quads currently into 2 detached triangles, we can't update quads with 2 different normals...
        public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, FPSCamera camera, GraphicsDevice graphicsDevice)
        {
            if (faceMesh.buffer != null)
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
                    Vector3 newPosition = positions[draggingIndex.Value];
                    float oldDistance = (newPosition - camera.GetPosition()).Length();
                    newPosition = camera.GetPosition() + camera.GetLookUnitVector(mouseState.X, mouseState.Y, graphicsDevice) * oldDistance;
                    newPosition.X = (float)Math.Round(newPosition.X * 4) / 4;
                    newPosition.Y = (float)Math.Round(newPosition.Y * 4) / 4;
                    newPosition.Z = (float)Math.Round(newPosition.Z * 4) / 4;
                    positions[draggingIndex.Value] = newPosition;
                    tracker.Update(draggingIndex.Value, newPosition);
                    faceMesh.Update(draggingIndex.Value, positions);
                    lineMesh.Update(draggingIndex.Value, positions);
                    pointMesh.Update(draggingIndex.Value, positions);
                }
            }
        }
    }
}

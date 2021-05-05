using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZComponents.Data;
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
    // g to move
    // grid base at y=0? which extends infinite and is thicker every 10
    // rgb xyz axis and widget
    // selecting mesh gives outline and fainter outline if certain depth in
    // undo/redo
    // saving
    // reverting
    // ctrl l to select all connected
    public class MeshTemplate : ZGameObject
    {
        FaceMesh faceMesh;
        LineMesh lineMesh;
        PointMesh pointMesh;
        VertexDataComponent vertexData;
        PointCollectionTracker tracker = new PointCollectionTracker();
        HashSet<int> selected = new HashSet<int>();

        public MeshTemplate()
        {
            vertexData = new VertexDataComponent() { saveColor = false };
            faceMesh = new FaceMesh() { vertexData = vertexData };
            lineMesh = new LineMesh() { vertexData = vertexData };
            pointMesh = new PointMesh() { vertexData = vertexData };
            vertexData.AddObserver(faceMesh);
            vertexData.AddObserver(lineMesh);
            vertexData.AddObserver(pointMesh);
            vertexData.AddObserver(tracker);
            Register(vertexData, faceMesh, lineMesh, pointMesh);
        }

        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            vertexData.Load(reader, graphicsDevice);
            var currLine = reader.ReadLine();
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
            faceMesh.MakeBuffer(graphicsDevice);
            lineMesh.MakeBuffer(graphicsDevice);
            pointMesh.MakeBuffer(graphicsDevice);
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

        Vector2? dragOrigin = null;
        Dictionary<int, Vector3> oldPositions = new Dictionary<int, Vector3>();
        public void Update(UIContext uiContext, AbstractCamera camera, bool editMode)
        {
            if (faceMesh.buffer != null && editMode)
            {
                if (uiContext.IsLeftMouseButtonPressed())
                {
                    if (dragOrigin == null)
                    {
                        int nearestIndex = tracker.GetNearest(camera.GetPosition(), camera.GetLookUnitVector(uiContext));
                        if (uiContext.IsCtrlPressed())
                        {

                        }
                        else if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift))
                        {
                            if (selected.Contains(nearestIndex))
                            {
                                vertexData.Update(nearestIndex, vertexData.positions[nearestIndex], Color.Black);
                                selected.Remove(nearestIndex);
                            }
                            else
                            {
                                vertexData.Update(nearestIndex, vertexData.positions[nearestIndex], Color.Orange);
                                selected.Add(nearestIndex);
                            }
                        }
                        else
                        {
                            foreach (var v in selected)
                            {
                                vertexData.Update(v, vertexData.positions[v], Color.Black);
                            }
                            selected.Clear();
                            selected.Add(nearestIndex);
                            vertexData.Update(nearestIndex, vertexData.positions[nearestIndex], Color.Orange);
                        }
                    }
                    else
                    {
                        dragOrigin = null;
                    }
                }
                if (uiContext.IsKeyPressed(Keys.Escape))
                {
                    dragOrigin = null;
                }
                if (uiContext.IsKeyPressed(Keys.G) && dragOrigin == null)
                {
                    dragOrigin = uiContext.MouseVector2;
                    oldPositions.Clear();
                    foreach (var s in selected) oldPositions.Add(s, vertexData.positions[s]);
                }
                if (selected.Count > 0 && dragOrigin != null)
                {
                    Vector3 sumOffset = Vector3.Zero;
                    foreach (var s in selected)
                    {
                        Vector3 currPosition = vertexData.positions[s];
                        sumOffset += camera.GetPerspectiveOffset(uiContext, currPosition, uiContext.MouseVector2 - dragOrigin.Value);
                    }
                    sumOffset /= selected.Count;
                    sumOffset.X = (float)Math.Round(sumOffset.X * 4) / 4;
                    sumOffset.Y = (float)Math.Round(sumOffset.Y * 4) / 4;
                    sumOffset.Z = (float)Math.Round(sumOffset.Z * 4) / 4;
                    foreach (var s in selected)
                    {
                        // get position and update tracker
                        Vector3 newPosition = oldPositions[s];
                        newPosition += sumOffset;
                        vertexData.Update(s, newPosition, Color.Orange);
                    }
                }
            }
        }
    }
}

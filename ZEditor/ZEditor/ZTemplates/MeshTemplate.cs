using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.DataStructures;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.Drawables;
using ZEditor.ZComponents.UI;
using ZEditor.ZControl;
using ZEditor.ZGraphics;
using ZEditor.ZManage;
using ZEditor.ZTemplates.Mesh;
using static ZEditor.ZComponents.Data.VertexDataComponent;

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
    public class MeshTemplate : ZGameObject, IVertexListHashObserver
    {
        private BoundingBox boundingBox = new BoundingBox();
        private FaceMesh faceMesh;
        private LineMesh lineMesh;
        private PointMesh pointMesh;
        private VertexDataComponent vertexData;
        private VertexListHashDataComponent polyData;
        private Dictionary<VertexData[], int> lineParentCounts = new Dictionary<VertexData[], int>(new ReversibleArrayEqualityComparer<VertexData>());
        private Dictionary<VertexData, int> pointParentCounts = new Dictionary<VertexData, int>();

        public MeshTemplate()
        {
            vertexData = new VertexDataComponent() { saveColor = false };
            polyData = new VertexListHashDataComponent() { vertexData = vertexData };
            polyData.AddObserver(this);
            faceMesh = new FaceMesh() { vertexData = vertexData };
            lineMesh = new LineMesh() { vertexData = vertexData };
            pointMesh = new PointMesh() { vertexData = vertexData };
            var tracker = new PointCollectionTracker<VertexData>(vertexData, x => x.position);
            Register(vertexData, polyData, faceMesh, lineMesh, pointMesh);
            // setup ui
            // TODO: these all need to be in reversible actions
            var selector = new Selector<VertexData>(new CameraSelectionProvider<VertexData>(tracker),
                x => { x.color = Color.Orange; RecalculateEverything(); },
                x => { x.color = Color.Black; RecalculateEverything(); }
            );
            CameraMouseTracker dragMouseTracker = new CameraMouseTracker() { stepSize = 0.25f };
            // TODO: maybe make event handlers so you can do += stuff...
            dragMouseTracker.OnStepDiff = x => { foreach (var s in selector.selected) s.position += x; RecalculateEverything(); };
            StateSwitcher switcher = new StateSwitcher(selector);
            switcher.AddKeyState(Trigger.G, dragMouseTracker, () =>
            {
                // translate vertices
                Vector3 selectedSum = Vector3.Zero;
                foreach (var s in selector.selected) selectedSum += s.position;
                dragMouseTracker.worldOrigin = selectedSum / selector.selected.Count;
                dragMouseTracker.mouseOrigin = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                dragMouseTracker.oldOffset = null;
            }, true);
            switcher.AddKeyState(Trigger.H, dragMouseTracker, () =>
            {
                // extrude
                Vector3 selectedSum = Vector3.Zero;
                foreach (var s in selector.selected) selectedSum += s.position;
                dragMouseTracker.worldOrigin = selectedSum / selector.selected.Count;
                dragMouseTracker.mouseOrigin = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                dragMouseTracker.oldOffset = null;
                var selectedPolys = GetSelectedPolys(selector);
                var clonedPolys = ClonePolys(selectedPolys);
                foreach (var p in clonedPolys) polyData.Add(p);
                SetSelected(selector, clonedPolys);
                var perim1 = GetPerimeterPieces(selectedPolys);
                var perim2 = GetPerimeterPieces(clonedPolys);
                for (int i = 0; i < perim1.Count; i++)
                {
                    polyData.Add(new VertexData[] { perim1[i][1], perim2[i][1], perim2[i][0], perim1[i][0] });
                }
                foreach (var p in selectedPolys) polyData.Remove(p);
            }, true);
            switcher.AddKeyState(Trigger.ShiftA, dragMouseTracker, () =>
            {
                // add a new unit plane and drag
                var v1 = new VertexData(new Vector3(0, 0, 0), Color.Black);
                var v2 = new VertexData(new Vector3(1, 0, 0), Color.Black);
                var v3 = new VertexData(new Vector3(1, 0, 1), Color.Black);
                var v4 = new VertexData(new Vector3(0, 0, 1), Color.Black);
                vertexData.AddRange(new[] { v1, v2, v3, v4 });
                var newPoly = new VertexData[] { v1, v2, v3, v4 };
                polyData.Add(newPoly);
                SetSelected(selector, new List<VertexData[]>() { newPoly });
                dragMouseTracker.worldOrigin = new Vector3(0.5f, 0, 0.5f);
                dragMouseTracker.mouseOrigin = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                dragMouseTracker.oldOffset = null;
                var selectedPolys = GetSelectedPolys(selector);
            }, true);
            Register(switcher);
            RegisterListener(new InputListener(Trigger.Delete, x =>
            {
                var selectedPolys = GetSelectedPolys(selector);
                foreach (var p in selectedPolys) polyData.Remove(p);
            }));
            RegisterListener(new InputListener(Trigger.F, x =>
            {
                var selectedLines = GetSelectedLines(selector);
                VertexData[] newPoly = new VertexData[selector.selected.Count];
                newPoly[0] = selectedLines[0][1];
                newPoly[1] = selectedLines[0][0];
                List<VertexData> remaining = selector.selected.ToList();
                remaining.Remove(newPoly[0]);
                remaining.Remove(newPoly[1]);
                for (int i = 2; i < newPoly.Length; i++)
                {
                    VertexData best = null;
                    double bestValue = -100;
                    foreach (var r in remaining)
                    {
                        Vector3 v1 = newPoly[i - 1].position - newPoly[i - 2].position;
                        Vector3 v2 = r.position - newPoly[i - 1].position;
                        v1.Normalize();
                        v2.Normalize();
                        double thisValue = Vector3.Dot(v1, v2);
                        if (thisValue > bestValue)
                        {
                            bestValue = thisValue;
                            best = r;
                        }
                    }
                    remaining.Remove(best);
                    newPoly[i] = best;
                }
                polyData.Add(newPoly);
            }));
            Register(new PlaneGrid());
        }

        // return the lines (in order) of unshared edges
        private List<VertexData[]> GetPerimeterPieces(List<VertexData[]> polys)
        {
            List<VertexData[]> perimPieces = new List<VertexData[]>();
            Dictionary<VertexData[], int> lineCounts = new Dictionary<VertexData[], int>(new ReversibleArrayEqualityComparer<VertexData>());
            foreach (var poly in polys)
            {
                for (int i = 0; i < poly.Length; i++)
                {
                    var line = new VertexData[] { poly[i], poly[(i + 1) % poly.Length] };
                    if (!lineCounts.ContainsKey(line)) lineCounts[line] = 0;
                    lineCounts[line]++;
                }
            }
            foreach (var poly in polys)
            {
                for (int i = 0; i < poly.Length; i++)
                {
                    var line = new VertexData[] { poly[i], poly[(i + 1) % poly.Length] };
                    if (lineCounts[line] == 1) perimPieces.Add(line);
                }
            }
            return perimPieces;
        }

        private void SetSelected(Selector<VertexData> selector, List<VertexData[]> polys)
        {
            selector.Clear();
            foreach (var poly in polys)
            {
                foreach (var v in poly)
                {
                    selector.Add(v);
                }
            }
        }

        private List<VertexData[]> GetLines(VertexData[] poly)
        {
            List<VertexData[]> lines = new List<VertexData[]>();
            for (int i = 0; i < poly.Length; i++)
            {
                lines.Add(new VertexData[] { poly[i], poly[(i + 1) % poly.Length] });
            }
            return lines;
        }

        private List<VertexData[]> GetSelectedLines(Selector<VertexData> selector)
        {
            var selected = new List<VertexData[]>();
            foreach (var poly in polyData.lists)
            {
                foreach (var line in GetLines(poly))
                {
                    if (line.All(x => selector.selected.Contains(x)))
                    {
                        selected.Add(line);
                    }
                }
            }
            return selected;
        }

        private List<VertexData[]> GetSelectedPolys(Selector<VertexData> selector)
        {
            return polyData.lists.Where(x => x.All(y => selector.selected.Contains(y))).ToList();
        }

        private List<VertexData[]> ClonePolys(List<VertexData[]> originalPolys)
        {
            var replacedVertices = new Dictionary<VertexData, VertexData>();
            var clonedPolys = new List<VertexData[]>();
            foreach (var poly in originalPolys)
            {
                VertexData[] clonedPoly = new VertexData[poly.Length];
                for (int i = 0; i < poly.Length; i++)
                {
                    if (!replacedVertices.ContainsKey(poly[i]))
                    {
                        replacedVertices[poly[i]] = new VertexData(poly[i].position, Color.Black);
                        vertexData.Add(replacedVertices[poly[i]]);
                    }
                    clonedPoly[i] = replacedVertices[poly[i]];
                }
                clonedPolys.Add(clonedPoly);
            }
            return clonedPolys;
        }

        public void Add(VertexData[] intList)
        {
            faceMesh.AddItem(intList);
            for (int i = 0; i < intList.Length; i++)
            {
                var line = new VertexData[] { intList[i], intList[(i + 1) % intList.Length] };
                if (!lineParentCounts.ContainsKey(line)) lineParentCounts.Add(line, 0);
                lineParentCounts[line]++;
                lineMesh.AddItem(line);
                if (!pointParentCounts.ContainsKey(intList[i])) pointParentCounts.Add(intList[i], 0);
                pointParentCounts[intList[i]]++;
                pointMesh.AddItem(intList[i]);
            }
            RecalculateEverything();
        }

        public void Remove(VertexData[] intList)
        {
            faceMesh.RemoveItem(intList);
            // TODO: make linemesh etc functions protected or sealed or something
            for (int i = 0; i < intList.Length; i++)
            {
                var line = new VertexData[] { intList[i], intList[(i + 1) % intList.Length] };
                lineParentCounts[line]--;
                if (lineParentCounts[line] == 0)
                {
                    lineParentCounts.Remove(line);
                    lineMesh.RemoveItem(line);
                }
                pointParentCounts[intList[i]]--;
                if (pointParentCounts[intList[i]] == 0)
                {
                    pointParentCounts.Remove(intList[i]);
                    pointMesh.RemoveItem(intList[i]);
                }
            }
            RecalculateEverything();
        }

        private void RecalculateEverything()
        {
            faceMesh.RecalculateEverything();
            lineMesh.RecalculateEverything();
            pointMesh.RecalculateEverything();
            var min = new Vector3(vertexData.Min(x => x.position.X), vertexData.Min(x => x.position.Y), vertexData.Min(x => x.position.Z));
            var max = new Vector3(vertexData.Max(x => x.position.X), vertexData.Max(x => x.position.Y), vertexData.Max(x => x.position.Z));
            boundingBox = new BoundingBox(min, max);
        }

        public override BoundingBox GetBoundingBox()
        {
            return boundingBox;
        }
    }
}

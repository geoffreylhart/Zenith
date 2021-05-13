using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.UI;
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
    public class MeshTemplate : ZGameObject, IIntListHashObserver
    {
        FaceMesh faceMesh;
        LineMesh lineMesh;
        PointMesh pointMesh;
        VertexDataComponent vertexData;
        IntListHashDataComponent polyData;
        PointCollectionTracker tracker = new PointCollectionTracker();

        public MeshTemplate()
        {
            vertexData = new VertexDataComponent() { saveColor = false };
            polyData = new IntListHashDataComponent();
            polyData.AddObserver(this);
            faceMesh = new FaceMesh() { vertexData = vertexData };
            lineMesh = new LineMesh() { vertexData = vertexData };
            pointMesh = new PointMesh() { vertexData = vertexData };
            vertexData.AddObserver(faceMesh);
            vertexData.AddObserver(lineMesh);
            vertexData.AddObserver(pointMesh);
            vertexData.AddObserver(tracker);
            Register(vertexData, polyData, faceMesh, lineMesh, pointMesh);
            // setup ui
            // TODO: these all need to be in reversible actions
            Selector selector = new Selector(new CameraSelectionProvider(tracker), x => vertexData.Update(x, vertexData.positions[x], Color.Orange), x => vertexData.Update(x, vertexData.positions[x], Color.Black));
            CameraMouseTracker cameraMouseTracker = new CameraMouseTracker();
            cameraMouseTracker.stepSize = 0.25f;
            // TODO: maybe make event handlers so you can do += stuff...
            cameraMouseTracker.OnStepDiff = x => { foreach (var s in selector.selected) vertexData.Update(s, vertexData.positions[s] + x, Color.Orange); };
            StateSwitcher switcher = new StateSwitcher(selector);
            switcher.AddKeyState(Keys.G, cameraMouseTracker, () =>
            {
                Vector3 selectedSum = Vector3.Zero;
                foreach (var s in selector.selected) selectedSum += vertexData.positions[s];
                cameraMouseTracker.worldOrigin = selectedSum / selector.selected.Count;
                cameraMouseTracker.mouseOrigin = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
            });
            Register(switcher);
        }

        public void Add(int[] intList)
        {
            faceMesh.AddItem(intList);
            for (int i = 0; i < intList.Length; i++)
            {
                lineMesh.AddItem(new int[] { intList[i], intList[(i + 1) % intList.Length] });
                pointMesh.AddItem(new int[] { intList[i] });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents
{
    // Controls:
    // - Right click a point to remove it
    // - Left click a point to drag it
    // - Left click on a line to add a point
    internal class SphericalGeometryEditor : DrawableGameComponent, IEditorGameComponent
    {
        private EditorCamera camera;
        private List<VectorHandle> shape;
        private Vector2 previewPoint;
        private static int HALF_SIZE = 4;
        private int draggingPointIndex = -1;
        private IndexType draggingPointIndexType = IndexType.BASE;
        private VertexBuffer vertexBuffer = null;

        private class VectorHandle
        {
            public Vector2 p;
            public Vector2 incoming; // relative to p
            public Vector2 outgoing;
            public HandleType handleType;

            public VectorHandle(float x, float y)
            {
                p = new Vector2(x, y);
                handleType = HandleType.SHARP;
                incoming = new Vector2(0, 0.02f);
                outgoing = new Vector2(0, -0.02f);
            }
        }

        private enum HandleType
        {
            FREE, SMOOTH, SHARP
        }

        private enum IndexType
        {
            BASE, INCOMING, OUTGOING
        }

        internal SphericalGeometryEditor(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
            shape = new List<VectorHandle>() { new VectorHandle(0, 0), new VectorHandle(0, 0.1f), new VectorHandle(0.1f, 0.1f), new VectorHandle(0.1f, 0) };
        }

        public override void Update(GameTime gameTime)
        {
            var mousePos = Mouse.GetState().Position;
            var mouseLongLat = camera.GetLatLongOfCoord(new Vector2(mousePos.X, mousePos.Y));
            if (mouseLongLat != null)
            {
                Vector2 mouseAsVec2 = new Vector2((float)mouseLongLat.X, (float)mouseLongLat.Y);
                bool leftJustPressed = UILayer.LeftPressed;
                bool rightJustPressed = UILayer.RightPressed;
                int bestIndex = -1;
                IndexType bestIndexType = IndexType.BASE;
                float bestDist = float.MaxValue;
                for (int i = 0; i < shape.Count; i++) // get closest line
                {
                    var p1 = shape[i].p;
                    var p2 = shape[(i + 1) % shape.Count].p;
                    float dist = AllMath.DistanceFromLineOrPoints(mouseAsVec2, p1, p2);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIndex = i;
                        bestIndexType = IndexType.BASE;
                    }
                    float dist2 = Vector2.Distance(shape[i].incoming + shape[i].p, mouseAsVec2);
                    float dist3 = Vector2.Distance(shape[i].outgoing + shape[i].p, mouseAsVec2);
                    if (dist2 < bestDist)
                    {
                        bestDist = dist2;
                        bestIndex = i;
                        bestIndexType = IndexType.INCOMING;
                    }
                    if (dist3 < bestDist)
                    {
                        bestDist = dist3;
                        bestIndex = i;
                        bestIndexType = IndexType.OUTGOING;
                    }
                }
                if (bestIndexType == IndexType.BASE)
                {
                    float t = AllMath.ProjectionTOnLine(mouseAsVec2, shape[bestIndex].p, shape[(bestIndex + 1) % shape.Count].p);
                    if (t < 0.1)
                    {
                        previewPoint = shape[bestIndex].p;
                        if (leftJustPressed)
                        {
                            draggingPointIndex = bestIndex;
                            draggingPointIndexType = IndexType.BASE;
                        }
                    }
                    else if (t > 0.9)
                    {
                        previewPoint = shape[(bestIndex + 1) % shape.Count].p;
                        if (leftJustPressed)
                        {
                            draggingPointIndex = (bestIndex + 1) % shape.Count;
                            draggingPointIndexType = IndexType.BASE;
                        }
                    }
                    else
                    {
                        previewPoint = shape[bestIndex].p + t * (shape[(bestIndex + 1) % shape.Count].p - shape[bestIndex].p);
                        if (leftJustPressed)
                        {
                            shape.Insert(bestIndex + 1, new VectorHandle(previewPoint.X, previewPoint.Y));
                            draggingPointIndex = bestIndex + 1;
                        }
                    }
                }
                if (bestIndexType == IndexType.INCOMING)
                {
                    previewPoint = shape[bestIndex].incoming + shape[bestIndex].p;
                    if (leftJustPressed)
                    {
                        draggingPointIndex = bestIndex;
                        draggingPointIndexType = IndexType.INCOMING;
                    }
                }
                if (bestIndexType == IndexType.OUTGOING)
                {
                    previewPoint = shape[bestIndex].outgoing + shape[bestIndex].p;
                    if (leftJustPressed)
                    {
                        draggingPointIndex = bestIndex;
                        draggingPointIndexType = IndexType.OUTGOING;
                    }
                }
                if (UILayer.LeftDown)
                {
                    if (draggingPointIndexType == IndexType.BASE) shape[draggingPointIndex].p = mouseAsVec2;
                    if (draggingPointIndexType == IndexType.INCOMING) shape[draggingPointIndex].incoming = mouseAsVec2 - shape[draggingPointIndex].p;
                    if (draggingPointIndexType == IndexType.OUTGOING) shape[draggingPointIndex].outgoing = mouseAsVec2 - shape[draggingPointIndex].p;
                }
                if (bestIndexType == IndexType.BASE && UILayer.RightPressed && !UILayer.LeftDown)
                {
                    float t = AllMath.ProjectionTOnLine(mouseAsVec2, shape[bestIndex].p, shape[(bestIndex + 1) % shape.Count].p);
                    if (t < 0.1)
                    {
                        shape.RemoveAt(bestIndex);
                    }
                    if (t > 0.9)
                    {
                        shape.RemoveAt((bestIndex + 1) % shape.Count);
                    }
                }
            }
            // TODO: going to have to double check all this logic if I ever use this class again
            if (UILayer.LeftDown || UILayer.RightDown || vertexBuffer == null) UpdateVertexBuffer();
        }

        private void UpdateVertexBuffer()
        {
            var shapeAsVertices = new List<VertexPosition>();
            for (int i = 0; i < shape.Count; i++)
            {
                var p1 = shape[i];
                var p4 = shape[(i + 1) % shape.Count];
                for (int j = 0; j < 10; j++)
                {
                    float t = j / 10.0f;
                    Vector2 p12 = p1.p + t * p1.outgoing;
                    Vector2 p23 = (p1.p + p1.outgoing) * (1 - t) + (p4.p + p4.incoming) * t;
                    Vector2 p34 = p4.p + (1 - t) * p4.incoming;
                    Vector2 p123 = p12 * (1 - t) + t * p23;
                    Vector2 p234 = p23 * (1 - t) + t * p34;
                    Vector2 curvePoint = p123 * (1 - t) + t * p234;
                    shapeAsVertices.Add(new VertexPosition(Vector3Helper.UnitSphere(curvePoint.X, curvePoint.Y)));
                }
            }
            shapeAsVertices.Add(shapeAsVertices[0]);
            vertexBuffer = new VertexBuffer(GraphicsDevice, VertexPosition.VertexDeclaration, shapeAsVertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(shapeAsVertices.ToArray());
        }

        public override void Draw(GameTime gameTime)
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            camera.ApplyMatrices(basicEffect3);
            foreach (var point in shape)
            {
                DrawPoint(point.p, Color.White);
                DrawPoint(point.incoming + point.p, Color.Orange);
                DrawPoint(point.outgoing + point.p, Color.Orange);
            }
            var shapeHandles = new List<VertexPositionColor>();
            foreach (var handle in shape)
            {
                shapeHandles.Add(new VertexPositionColor(Vector3Helper.UnitSphere(handle.p.X, handle.p.Y), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(Vector3Helper.UnitSphere(handle.p.X + handle.incoming.X, handle.p.Y + handle.incoming.Y), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(Vector3Helper.UnitSphere(handle.p.X, handle.p.Y), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(Vector3Helper.UnitSphere(handle.p.X + handle.outgoing.X, handle.p.Y + handle.outgoing.Y), Color.Orange));
            }
            if (vertexBuffer != null)
            {
                GraphicsDevice.SetVertexBuffer(vertexBuffer);
                foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, vertexBuffer.VertexCount);
                }
            }
            basicEffect3.VertexColorEnabled = true;
            foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, shapeHandles.ToArray());
            }
            DrawPoint(previewPoint, Color.Red);
        }

        private void DrawPoint(Vector2 point, Color color)
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.VertexColorEnabled = true;
            basicEffect3.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1, 1000);
            List<VertexPositionColor> markerVertices = new List<VertexPositionColor>();
            Vector3 v = Vector3Helper.UnitSphere(point.X, point.Y);
            v = camera.Project(v);
            if (v.Z < 1)
            {
                GraphicsBasic.DrawScreenRect(GraphicsDevice, v.X - HALF_SIZE, v.Y - HALF_SIZE, HALF_SIZE * 2, HALF_SIZE * 2, color);
            }
        }

        public List<string> GetDebugInfo()
        {
            return new List<string>() { "Controls: Left/Right click" };
        }

        public List<IUIComponent> GetSettings()
        {
            return new List<IUIComponent>();
        }

        public List<IEditorGameComponent> GetSubComponents()
        {
            return new List<IEditorGameComponent>();
        }
    }
}

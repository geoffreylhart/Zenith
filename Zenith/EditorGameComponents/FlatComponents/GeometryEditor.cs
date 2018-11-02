using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class GeometryEditor : IFlatComponent
    {
        private List<VectorHandle> shape;
        private Vector2 previewPoint;
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

        public GeometryEditor()
        {
            shape = new List<VectorHandle>() { new VectorHandle(0, 0), new VectorHandle(0, 0.1f), new VectorHandle(0.1f, 0.1f), new VectorHandle(0.1f, 0) };
        }

        public void Draw(GraphicsDevice graphicsDevice, double minX, double maxX, double minY, double maxY)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
            basicEffect.VertexColorEnabled = true;
            foreach (var point in shape)
            {
                DrawPoint(graphicsDevice, basicEffect, point.p, Color.White);
                DrawPoint(graphicsDevice, basicEffect, point.incoming + point.p, Color.Orange);
                DrawPoint(graphicsDevice, basicEffect, point.outgoing + point.p, Color.Orange);
            }
            var shapeHandles = new List<VertexPositionColor>();
            float z = -10;
            foreach (var handle in shape)
            {
                shapeHandles.Add(new VertexPositionColor(new Vector3(handle.p, z), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(new Vector3(handle.p + handle.incoming, z), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(new Vector3(handle.p + handle.outgoing, z), Color.Orange));
            }
            if (vertexBuffer != null)
            {
                graphicsDevice.SetVertexBuffer(vertexBuffer);
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, vertexBuffer.VertexCount);
                }
            }
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, shapeHandles.ToArray());
            }
            DrawPoint(graphicsDevice, basicEffect, previewPoint, Color.Red);

            // TODO: going to have to double check all this logic if I ever use this class again
            if (UILayer.LeftDown || UILayer.RightDown || vertexBuffer == null) UpdateVertexBuffer(graphicsDevice);
        }

        public void Update(double mouseX, double mouseY, double cameraZoom)
        {
            var mouseAsVec2 = new Vector2((float)mouseX, (float)mouseY);
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

        private void UpdateVertexBuffer(GraphicsDevice graphicsDevice)
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
            vertexBuffer = new VertexBuffer(graphicsDevice, VertexPosition.VertexDeclaration, shapeAsVertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(shapeAsVertices.ToArray());
        }

        private void DrawPoint(GraphicsDevice graphicsDevice, BasicEffect basicEffect, Vector2 v, Color color)
        {
            float HALF_SIZE = 0.01f;
            GraphicsBasic.DrawRect(graphicsDevice, basicEffect, v.X - HALF_SIZE, v.Y - HALF_SIZE, HALF_SIZE * 2, HALF_SIZE * 2, color);
        }
    }
}

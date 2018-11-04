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
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class GeometryEditor : IFlatComponent
    {
        private List<VectorHandle> shape;
        private SphereVector previewPoint;
        private int draggingPointIndex = -1;
        private IndexType draggingPointIndexType = IndexType.BASE;
        private VertexBuffer vertexBuffer = null;
        private bool updateVertexBuffer = false;

        private class VectorHandle
        {
            public SphereVector p;
            public SphereVector incoming;
            public SphereVector outgoing;
            public HandleType handleType;

            public VectorHandle(SphereVector v)
            {
                p = v;
                handleType = HandleType.SHARP;
                incoming = v.WalkNorth(0.01f);
                outgoing = v.WalkNorth(-0.01f);
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
            LongLat[] longLats = new[] { new LongLat(0, 0), new LongLat(0, 0.1f), new LongLat(0.1f, 0.1f), new LongLat(0.1f, 0) };
            shape = new List<VectorHandle>();
            foreach (var longLat in longLats) shape.Add(new VectorHandle(longLat.ToSphereVector()));
        }

        public void Draw(GraphicsDevice graphicsDevice, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            float halfSize = (float)(0.2 * Math.Pow(0.5, cameraZoom));
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
            basicEffect.VertexColorEnabled = true;
            foreach (var point in shape)
            {
                DrawPoint(graphicsDevice, basicEffect, halfSize, point.p.ToLongLat(), Color.White);
                DrawPoint(graphicsDevice, basicEffect, halfSize, point.incoming.ToLongLat(), Color.Orange);
                DrawPoint(graphicsDevice, basicEffect, halfSize, point.outgoing.ToLongLat(), Color.Orange);
            }
            var shapeHandles = new List<VertexPositionColor>();
            float z = -10;
            foreach (var handle in shape) // for drawing lines from in/out handles to the base
            {
                shapeHandles.Add(new VertexPositionColor(new Vector3(handle.p.ToLongLat(), z), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(new Vector3(handle.incoming.ToLongLat(), z), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(new Vector3(handle.p.ToLongLat(), z), Color.Orange));
                shapeHandles.Add(new VertexPositionColor(new Vector3(handle.outgoing.ToLongLat(), z), Color.Orange));
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
            if (previewPoint != null)
            {
                DrawPoint(graphicsDevice, basicEffect, halfSize, previewPoint.ToLongLat(), Color.Red);
            }

            // TODO: going to have to double check all this logic if I ever use this class again
            if (updateVertexBuffer || vertexBuffer == null)
            {
                updateVertexBuffer = false;
                UpdateVertexBuffer(graphicsDevice);
            }
        }

        public void Update(double mouseX, double mouseY, double cameraZoom)
        {
            SphereVector coord3D = new LongLat(mouseX, mouseY).ToSphereVector(); // TODO: just pass this coord directly from planet component
            previewPoint = null;
            var mouseAsVec2 = new Vector2((float)mouseX, (float)mouseY);
            int bestIndex = -1;
            IndexType bestIndexType = IndexType.BASE;
            double bestDist = double.MaxValue;
            for (int i = 0; i < shape.Count; i++) // get closest line
            {
                double distBase = coord3D.Distance(shape[i].p);
                double distIncoming = coord3D.Distance(shape[i].incoming);
                double distOutgoing = coord3D.Distance(shape[i].outgoing);
                if (distBase < bestDist)
                {
                    bestDist = distBase;
                    bestIndex = i;
                    bestIndexType = IndexType.BASE;
                }
                if (distIncoming < bestDist)
                {
                    bestDist = distIncoming;
                    bestIndex = i;
                    bestIndexType = IndexType.INCOMING;
                }
                if (distOutgoing < bestDist)
                {
                    bestDist = distOutgoing;
                    bestIndex = i;
                    bestIndexType = IndexType.OUTGOING;
                }
            }
            double maxDist = 1 * Math.Pow(0.5, cameraZoom);
            if (bestDist > maxDist) return;
            previewPoint = bestIndexType == IndexType.BASE ? shape[bestIndex].p : (bestIndexType == IndexType.INCOMING ? shape[bestIndex].incoming : shape[bestIndex].outgoing);
            if (UILayer.LeftPressed)
            {
                draggingPointIndex = bestIndex;
                draggingPointIndexType = bestIndexType;
            }
            if (UILayer.LeftDown)
            {
                if (draggingPointIndexType == IndexType.BASE) shape[draggingPointIndex].p = coord3D;
                if (draggingPointIndexType == IndexType.INCOMING) shape[draggingPointIndex].incoming = coord3D;
                if (draggingPointIndexType == IndexType.OUTGOING) shape[draggingPointIndex].outgoing = coord3D;
                updateVertexBuffer = true;
            }
            if (bestIndexType == IndexType.BASE && UILayer.RightPressed && !UILayer.LeftDown)
            {
                shape.RemoveAt(bestIndex);
                updateVertexBuffer = true;
            }
            UILayer.ConsumeLeft();
            UILayer.ConsumeRight();
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
                    SphereVector p12 = p1.p.WalkTowardsPortion(p1.outgoing, t);
                    SphereVector p23 = p1.outgoing.WalkTowardsPortion(p4.incoming, t);
                    SphereVector p34 = p4.incoming.WalkTowardsPortion(p4.p, t);
                    SphereVector p123 = p12.WalkTowardsPortion(p23, t);
                    SphereVector p234 = p23.WalkTowardsPortion(p34, t);
                    SphereVector curvePoint = p123.WalkTowardsPortion(p234, t);
                    shapeAsVertices.Add(new VertexPosition(new Vector3(curvePoint.ToLongLat(), -10f)));
                }
            }
            shapeAsVertices.Add(shapeAsVertices[0]);
            vertexBuffer = new VertexBuffer(graphicsDevice, VertexPosition.VertexDeclaration, shapeAsVertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(shapeAsVertices.ToArray());
        }

        private void DrawPoint(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double halfSize, Vector2d v, Color color)
        {
            GraphicsBasic.DrawRect(graphicsDevice, basicEffect, v.X - halfSize, v.Y - halfSize, halfSize * 2, halfSize * 2, color);
        }
    }
}

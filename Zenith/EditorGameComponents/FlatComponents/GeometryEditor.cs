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

            public SphereVector GetPseudoIncoming()
            {
                if (handleType == HandleType.FREE)
                {
                    return incoming;
                }
                if (handleType == HandleType.SMOOTH)
                {
                    // TODO: review logic
                    double incomingPortion = incoming.Distance(p) / (incoming.Distance(p) + outgoing.Distance(p));
                    SphereVector newv = new SphereVector((incoming * incomingPortion + outgoing.FlipOver(p) * (1 - incomingPortion)).Normalized());
                    return new SphereVector(((newv - p) * incoming.Distance(p) / newv.Distance(p) + p).Normalized());
                }
                if (handleType == HandleType.SHARP)
                {
                    return p;
                }
                throw new NotImplementedException();
            }

            public SphereVector GetPseudoOutgoing()
            {
                if (handleType == HandleType.FREE)
                {
                    return outgoing;
                }
                if (handleType == HandleType.SMOOTH)
                {
                    // TODO: review logic
                    double outgoingPortion = outgoing.Distance(p) / (incoming.Distance(p) + outgoing.Distance(p));
                    SphereVector newv = new SphereVector((outgoing * outgoingPortion + incoming.FlipOver(p) * (1 - outgoingPortion)).Normalized());
                    return new SphereVector(((newv - p) * outgoing.Distance(p) / newv.Distance(p) + p).Normalized());
                }
                if (handleType == HandleType.SHARP)
                {
                    return p;
                }
                throw new NotImplementedException();
            }

            public SphereVector CurveTowards(VectorHandle v, double t)
            {
                SphereVector p12 = p.WalkTowardsPortion(GetPseudoOutgoing(), t);
                SphereVector p23 = GetPseudoOutgoing().WalkTowardsPortion(v.GetPseudoIncoming(), t);
                SphereVector p34 = v.GetPseudoIncoming().WalkTowardsPortion(v.p, t);
                SphereVector p123 = p12.WalkTowardsPortion(p23, t);
                SphereVector p234 = p23.WalkTowardsPortion(p34, t);
                return p123.WalkTowardsPortion(p234, t);
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
                if (point.handleType == HandleType.FREE || point.handleType == HandleType.SMOOTH)
                {
                    DrawPoint(graphicsDevice, basicEffect, halfSize, point.GetPseudoIncoming().ToLongLat(), Color.Orange);
                    DrawPoint(graphicsDevice, basicEffect, halfSize, point.GetPseudoOutgoing().ToLongLat(), Color.Orange);
                }
            }
            var shapeHandles = new List<VertexPositionColor>();
            float z = -10;
            foreach (var handle in shape) // for drawing lines from in/out handles to the base
            {
                if (handle.handleType == HandleType.FREE || handle.handleType == HandleType.SMOOTH)
                {
                    shapeHandles.Add(new VertexPositionColor(new Vector3(handle.p.ToLongLat(), z), Color.Orange));
                    shapeHandles.Add(new VertexPositionColor(new Vector3(handle.GetPseudoIncoming().ToLongLat(), z), Color.Orange));
                    shapeHandles.Add(new VertexPositionColor(new Vector3(handle.p.ToLongLat(), z), Color.Orange));
                    shapeHandles.Add(new VertexPositionColor(new Vector3(handle.GetPseudoOutgoing().ToLongLat(), z), Color.Orange));
                }
            }
            if (vertexBuffer != null)
            {
                graphicsDevice.SetVertexBuffer(vertexBuffer);
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, vertexBuffer.VertexCount - 1);
                }
            }
            if (shapeHandles.Count > 0)
            {
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, shapeHandles.ToArray());
                }
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
            if (!UILayer.LeftDown) draggingPointIndex = -1;
            SphereVector coord3D = new LongLat(mouseX, mouseY).ToSphereVector(); // TODO: just pass this coord directly from planet component
            previewPoint = null;
            var mouseAsVec2 = new Vector2((float)mouseX, (float)mouseY);
            int bestIndex = -1;
            IndexType bestIndexType = IndexType.BASE;
            double bestDist = double.MaxValue;
            for (int i = 0; i < shape.Count; i++) // get closest line
            {
                double distBase = coord3D.Distance(shape[i].p);
                if (distBase < bestDist)
                {
                    bestDist = distBase;
                    bestIndex = i;
                    bestIndexType = IndexType.BASE;
                }
                if (shape[i].handleType == HandleType.FREE || shape[i].handleType == HandleType.SMOOTH)
                {
                    double distIncoming = coord3D.Distance(shape[i].GetPseudoIncoming());
                    double distOutgoing = coord3D.Distance(shape[i].GetPseudoOutgoing());
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
            }
            double maxDist = 1 * Math.Pow(0.5, cameraZoom);
            if (bestDist < maxDist)
            {
                previewPoint = bestIndexType == IndexType.BASE ? shape[bestIndex].p : (bestIndexType == IndexType.INCOMING ? shape[bestIndex].GetPseudoIncoming() : shape[bestIndex].GetPseudoOutgoing());
                if (UILayer.LeftPressed)
                {
                    draggingPointIndex = bestIndex;
                    draggingPointIndexType = bestIndexType;
                }
                if (UILayer.LeftQuickClicked)
                {
                    shape[bestIndex].handleType++;
                    if ((int)shape[bestIndex].handleType == 3)
                    {
                        shape[bestIndex].handleType = 0;
                    }
                    updateVertexBuffer = true;
                }
            }
            if (draggingPointIndex >= 0)
            {
                if (draggingPointIndexType == IndexType.BASE)
                {
                    var diff = coord3D - shape[draggingPointIndex].p;
                    shape[draggingPointIndex].p = coord3D;
                    shape[draggingPointIndex].incoming = new SphereVector((shape[draggingPointIndex].incoming + diff).Normalized());
                    shape[draggingPointIndex].outgoing = new SphereVector((shape[draggingPointIndex].outgoing + diff).Normalized());
                }
                if (draggingPointIndexType == IndexType.INCOMING)
                {
                    shape[draggingPointIndex].incoming = coord3D;
                    if (shape[draggingPointIndex].handleType == HandleType.SMOOTH)
                    {
                        shape[draggingPointIndex].outgoing = shape[draggingPointIndex].p.WalkTowards(coord3D, -shape[draggingPointIndex].p.Distance(shape[draggingPointIndex].outgoing));
                    }
                }
                if (draggingPointIndexType == IndexType.OUTGOING)
                {
                    shape[draggingPointIndex].outgoing = coord3D;
                    if (shape[draggingPointIndex].handleType == HandleType.SMOOTH)
                    {
                        shape[draggingPointIndex].incoming = shape[draggingPointIndex].p.WalkTowards(coord3D, -shape[draggingPointIndex].p.Distance(shape[draggingPointIndex].incoming));
                    }
                }
                updateVertexBuffer = true;
            }
            if (bestDist < maxDist)
            {
                if (bestIndexType == IndexType.BASE && UILayer.RightPressed && !UILayer.LeftDown)
                {
                    shape.RemoveAt(bestIndex);
                    updateVertexBuffer = true;
                }
                UILayer.ConsumeLeft();
                UILayer.ConsumeRight();
            }
            else
            {
                bestDist = double.MaxValue;
                for (int i = 0; i < shape.Count; i++)
                {
                    SphereVector halfPoint = shape[i].CurveTowards(shape[(i + 1) % shape.Count], 0.5);
                    double distBase = coord3D.Distance(halfPoint);
                    if (distBase < bestDist)
                    {
                        bestDist = distBase;
                        bestIndex = i;
                    }
                }
                if (bestDist < maxDist)
                {
                    previewPoint = shape[bestIndex].CurveTowards(shape[(bestIndex + 1) % shape.Count], 0.5);
                    if (UILayer.LeftPressed)
                    {
                        VectorHandle newH = new VectorHandle(previewPoint);
                        newH.handleType = HandleType.SMOOTH;
                        // just guessing the new handle positions
                        VectorHandle p1 = shape[bestIndex];
                        VectorHandle p4 = shape[(bestIndex + 1) % shape.Count];
                        SphereVector p12 = p1.p.WalkTowardsPortion(p1.GetPseudoOutgoing(), 0.5);
                        SphereVector p23 = p1.GetPseudoOutgoing().WalkTowardsPortion(p4.GetPseudoIncoming(), 0.5);
                        SphereVector p34 = p4.GetPseudoIncoming().WalkTowardsPortion(p4.p, 0.5);
                        SphereVector p123 = p12.WalkTowardsPortion(p23, 0.5);
                        SphereVector p234 = p23.WalkTowardsPortion(p34, 0.5);
                        newH.incoming = p123.WalkTowardsPortion(p234, 0.25);
                        newH.outgoing = p234.WalkTowardsPortion(p123, 0.25);
                        p1.outgoing = p1.outgoing.WalkTowardsPortion(p1.p, 0.5);
                        p4.incoming = p4.incoming.WalkTowardsPortion(p4.p, 0.5);
                        shape.Insert(bestIndex + 1, newH);
                        draggingPointIndex = bestIndex + 1;
                        draggingPointIndexType = IndexType.BASE;
                    }
                    UILayer.ConsumeLeft();
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
                    SphereVector curvePoint = p1.CurveTowards(p4, t);
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

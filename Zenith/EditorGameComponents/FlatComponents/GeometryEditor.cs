using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class GeometryEditor : IFlatComponent
    {
        private static bool TESSELATE = false;
        private List<Shape> shapes = new List<Shape>();
        private SphereVector previewPoint;
        private int draggingPointShapeIndex = -1;
        private int draggingPointIndex = -1;
        private IndexType draggingPointIndexType = IndexType.BASE;
        private VertexBuffer landVertexBuffer = null;

        private class Shape
        {
            public List<VectorHandle> shape;
            public VertexBuffer vertexBuffer = null;
            public bool updateVertexBuffer = false;
        }

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
            AddNewShape(new SphereVector(0, -1, 0));
        }

        private void AddNewShape(SphereVector center)
        {
            Shape newShape = new Shape();
            newShape.shape = new List<VectorHandle>();
            VectorHandle handle1 = new VectorHandle(center.WalkNorth(0.1));
            handle1.handleType = HandleType.SMOOTH;
            handle1.incoming = handle1.p.WalkEast(-0.1);
            handle1.outgoing = handle1.p.WalkEast(0.1);
            VectorHandle handle2 = new VectorHandle(center.WalkNorth(-0.1));
            handle2.handleType = HandleType.SMOOTH;
            handle2.incoming = handle2.p.WalkEast(0.1);
            handle2.outgoing = handle2.p.WalkEast(-0.1);
            newShape.shape.Add(handle1);
            newShape.shape.Add(handle2);
            shapes.Add(newShape);
        }

        public void Draw(GraphicsDevice graphicsDevice, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            float halfSize = (float)(0.2 * Math.Pow(0.5, cameraZoom));
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
            basicEffect.VertexColorEnabled = true;
            foreach (var shape in shapes)
            {
                foreach (var point in shape.shape)
                {
                    DrawPoint(graphicsDevice, basicEffect, halfSize, point.p.ToLongLat(), Color.White);
                    if (point.handleType == HandleType.FREE || point.handleType == HandleType.SMOOTH)
                    {
                        DrawPoint(graphicsDevice, basicEffect, halfSize, point.GetPseudoIncoming().ToLongLat(), Color.Orange);
                        DrawPoint(graphicsDevice, basicEffect, halfSize, point.GetPseudoOutgoing().ToLongLat(), Color.Orange);
                    }
                }
            }
            var shapeHandles = new List<VertexPositionColor>();
            float z = -10;
            foreach (var shape in shapes)
            {
                foreach (var handle in shape.shape) // for drawing lines from in/out handles to the base
                {
                    if (handle.handleType == HandleType.FREE || handle.handleType == HandleType.SMOOTH)
                    {
                        AddTwoLongLatLinesIfNecessary(shapeHandles, handle.p, handle.GetPseudoIncoming(), Color.Orange);
                        AddTwoLongLatLinesIfNecessary(shapeHandles, handle.p, handle.GetPseudoOutgoing(), Color.Orange);
                    }
                }
            }
            if (landVertexBuffer != null)
            {
                graphicsDevice.SetVertexBuffer(landVertexBuffer);
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, landVertexBuffer.VertexCount - 2);
                }
            }
            foreach (var shape in shapes)
            {
                if (shape.vertexBuffer != null)
                {
                    graphicsDevice.SetVertexBuffer(shape.vertexBuffer);
                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, shape.vertexBuffer.VertexCount / 2);
                    }
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
            bool updateMainVertexBuffer = false;
            foreach (var shape in shapes)
            {
                if (shape.updateVertexBuffer || shape.vertexBuffer == null)
                {
                    shape.updateVertexBuffer = false;
                    updateMainVertexBuffer = true;
                    UpdateVertexBuffer(graphicsDevice, shape);
                }
            }
            if (updateMainVertexBuffer && TESSELATE)
            {
                UpdateMainVertexBuffer(graphicsDevice);
            }
        }

        private void AddTwoLongLatLinesIfNecessary(List<VertexPositionColor> vertexList, SphereVector v1, SphereVector v2, Color color)
        {
            LongLat v1l = v1.ToLongLat();
            LongLat v2l = v2.ToLongLat();
            if (Math.Abs(v1l.X - v2l.X) < Math.PI)
            {
                vertexList.Add(new VertexPositionColor(new Vector3(v1l, -10f), color));
                vertexList.Add(new VertexPositionColor(new Vector3(v2l, -10f), color));
            }
            else
            {
                LongLat minP = v1l.X < v2l.X ? v1l : v2l;
                LongLat maxP = v1l.X < v2l.X ? v2l : v1l;
                vertexList.Add(new VertexPositionColor(new Vector3(minP, -10f), color));
                vertexList.Add(new VertexPositionColor(new Vector3(maxP - new Vector2((float)(2 * Math.PI), 0), -10f), color));
                vertexList.Add(new VertexPositionColor(new Vector3(minP + new Vector2((float)(2 * Math.PI), 0), -10f), color));
                vertexList.Add(new VertexPositionColor(new Vector3(maxP, -10f), color));
            }
        }

        private void UpdateMainVertexBuffer(GraphicsDevice graphicsDevice)
        {
            List<VertexPositionColor> triangles = new List<VertexPositionColor>();
            float z = -10f;
            var tess = new LibTessDotNet.Tess();
            foreach (var shape in shapes)
            {
                var contour = new LibTessDotNet.ContourVertex[shape.shape.Count * 10];
                for (int i = 0; i < shape.shape.Count; i++)
                {
                    VectorHandle p1 = shape.shape[i];
                    VectorHandle p4 = shape.shape[(i + 1) % shape.shape.Count];
                    for (int j = 0; j < 10; j++)
                    {
                        float t = j / 10.0f;
                        SphereVector curvePoint = p1.CurveTowards(p4, t);
                        LongLat asLongLat = curvePoint.ToLongLat();
                        contour[i * 10 + j].Position = new LibTessDotNet.Vec3 { X = (float)asLongLat.X, Y = (float)asLongLat.Y, Z = 0 };
                    }
                }
                tess.AddContour(contour, LibTessDotNet.ContourOrientation.Original);
            }
            tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);
            for (int i = 0; i < tess.ElementCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var pos = tess.Vertices[tess.Elements[i * 3 + j]].Position;
                    triangles.Add(new VertexPositionColor(new Vector3(pos.X, pos.Y, z), Color.Green));
                }
            }
            if (landVertexBuffer != null) landVertexBuffer.Dispose();
            landVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, triangles.Count, BufferUsage.WriteOnly);
            landVertexBuffer.SetData(triangles.ToArray());
        }

        public void Update(double mouseX, double mouseY, double cameraZoom)
        {
            if (!UILayer.LeftDown) draggingPointIndex = -1;
            SphereVector coord3D = new LongLat(mouseX, mouseY).ToSphereVector(); // TODO: just pass this coord directly from planet component
            previewPoint = null;
            var mouseAsVec2 = new Vector2((float)mouseX, (float)mouseY);
            int bestShapeIndex = -1;
            int bestIndex = -1;
            IndexType bestIndexType = IndexType.BASE;
            double bestDist = double.MaxValue;
            for (int j = 0; j < shapes.Count; j++)
            {
                for (int i = 0; i < shapes[j].shape.Count; i++) // get closest line
                {
                    double distBase = coord3D.Distance(shapes[j].shape[i].p);
                    if (distBase < bestDist)
                    {
                        bestDist = distBase;
                        bestIndex = i;
                        bestShapeIndex = j;
                        bestIndexType = IndexType.BASE;
                    }
                    if (shapes[j].shape[i].handleType == HandleType.FREE || shapes[j].shape[i].handleType == HandleType.SMOOTH)
                    {
                        double distIncoming = coord3D.Distance(shapes[j].shape[i].GetPseudoIncoming());
                        double distOutgoing = coord3D.Distance(shapes[j].shape[i].GetPseudoOutgoing());
                        if (distIncoming < bestDist)
                        {
                            bestDist = distIncoming;
                            bestIndex = i;
                            bestShapeIndex = j;
                            bestIndexType = IndexType.INCOMING;
                        }
                        if (distOutgoing < bestDist)
                        {
                            bestDist = distOutgoing;
                            bestIndex = i;
                            bestShapeIndex = j;
                            bestIndexType = IndexType.OUTGOING;
                        }
                    }
                }
            }
            double maxDist = 1 * Math.Pow(0.5, cameraZoom);
            if (bestDist < maxDist)
            {
                previewPoint = bestIndexType == IndexType.BASE ? shapes[bestShapeIndex].shape[bestIndex].p : (bestIndexType == IndexType.INCOMING ? shapes[bestShapeIndex].shape[bestIndex].GetPseudoIncoming() : shapes[bestShapeIndex].shape[bestIndex].GetPseudoOutgoing());
                if (UILayer.LeftPressed)
                {
                    draggingPointIndex = bestIndex;
                    draggingPointIndexType = bestIndexType;
                }
                if (UILayer.LeftQuickClicked)
                {
                    shapes[bestShapeIndex].shape[bestIndex].handleType++;
                    if ((int)shapes[bestShapeIndex].shape[bestIndex].handleType == 3)
                    {
                        shapes[bestShapeIndex].shape[bestIndex].handleType = 0;
                    }
                    shapes[bestShapeIndex].updateVertexBuffer = true;
                }
            }
            if (draggingPointIndex >= 0)
            {
                if (draggingPointIndexType == IndexType.BASE)
                {
                    var diff = coord3D - shapes[bestShapeIndex].shape[draggingPointIndex].p;
                    shapes[bestShapeIndex].shape[draggingPointIndex].p = coord3D;
                    shapes[bestShapeIndex].shape[draggingPointIndex].incoming = new SphereVector((shapes[bestShapeIndex].shape[draggingPointIndex].incoming + diff).Normalized());
                    shapes[bestShapeIndex].shape[draggingPointIndex].outgoing = new SphereVector((shapes[bestShapeIndex].shape[draggingPointIndex].outgoing + diff).Normalized());
                }
                if (draggingPointIndexType == IndexType.INCOMING)
                {
                    shapes[bestShapeIndex].shape[draggingPointIndex].incoming = coord3D;
                    if (shapes[bestShapeIndex].shape[draggingPointIndex].handleType == HandleType.SMOOTH)
                    {
                        shapes[bestShapeIndex].shape[draggingPointIndex].outgoing = shapes[bestShapeIndex].shape[draggingPointIndex].p.WalkTowards(coord3D, -shapes[bestShapeIndex].shape[draggingPointIndex].p.Distance(shapes[bestShapeIndex].shape[draggingPointIndex].outgoing));
                    }
                }
                if (draggingPointIndexType == IndexType.OUTGOING)
                {
                    shapes[bestShapeIndex].shape[draggingPointIndex].outgoing = coord3D;
                    if (shapes[bestShapeIndex].shape[draggingPointIndex].handleType == HandleType.SMOOTH)
                    {
                        shapes[bestShapeIndex].shape[draggingPointIndex].incoming = shapes[bestShapeIndex].shape[draggingPointIndex].p.WalkTowards(coord3D, -shapes[bestShapeIndex].shape[draggingPointIndex].p.Distance(shapes[bestShapeIndex].shape[draggingPointIndex].incoming));
                    }
                }
                shapes[bestShapeIndex].updateVertexBuffer = true;
            }
            if (bestDist < maxDist)
            {
                if (bestIndexType == IndexType.BASE && UILayer.RightPressed && !UILayer.LeftDown)
                {
                    shapes[bestShapeIndex].shape.RemoveAt(bestIndex);
                    shapes[bestShapeIndex].updateVertexBuffer = true;
                    if (shapes[bestShapeIndex].shape.Count == 0) shapes.RemoveAt(bestShapeIndex);
                }
                UILayer.ConsumeLeft();
                UILayer.ConsumeRight();
            }
            else
            {
                bestDist = double.MaxValue;
                for (int j = 0; j < shapes.Count; j++)
                {
                    for (int i = 0; i < shapes[j].shape.Count; i++)
                    {
                        SphereVector halfPoint = shapes[j].shape[i].CurveTowards(shapes[j].shape[(i + 1) % shapes[j].shape.Count], 0.5);
                        double distBase = coord3D.Distance(halfPoint);
                        if (distBase < bestDist)
                        {
                            bestDist = distBase;
                            bestShapeIndex = j;
                            bestIndex = i;
                        }
                    }
                }
                if (bestDist < maxDist)
                {
                    previewPoint = shapes[bestShapeIndex].shape[bestIndex].CurveTowards(shapes[bestShapeIndex].shape[(bestIndex + 1) % shapes[bestShapeIndex].shape.Count], 0.5);
                    if (UILayer.LeftPressed)
                    {
                        VectorHandle newH = new VectorHandle(previewPoint);
                        newH.handleType = HandleType.SMOOTH;
                        // just guessing the new handle positions
                        VectorHandle p1 = shapes[bestShapeIndex].shape[bestIndex];
                        VectorHandle p4 = shapes[bestShapeIndex].shape[(bestIndex + 1) % shapes[bestShapeIndex].shape.Count];
                        SphereVector p12 = p1.p.WalkTowardsPortion(p1.GetPseudoOutgoing(), 0.5);
                        SphereVector p23 = p1.GetPseudoOutgoing().WalkTowardsPortion(p4.GetPseudoIncoming(), 0.5);
                        SphereVector p34 = p4.GetPseudoIncoming().WalkTowardsPortion(p4.p, 0.5);
                        SphereVector p123 = p12.WalkTowardsPortion(p23, 0.5);
                        SphereVector p234 = p23.WalkTowardsPortion(p34, 0.5);
                        newH.incoming = p123.WalkTowardsPortion(p234, 0.25);
                        newH.outgoing = p234.WalkTowardsPortion(p123, 0.25);
                        p1.outgoing = p1.outgoing.WalkTowardsPortion(p1.p, 0.5);
                        p4.incoming = p4.incoming.WalkTowardsPortion(p4.p, 0.5);
                        shapes[bestShapeIndex].shape.Insert(bestIndex + 1, newH);
                        draggingPointIndex = bestIndex + 1;
                        draggingPointIndexType = IndexType.BASE;
                    }
                    UILayer.ConsumeLeft();
                }
                else
                {
                    if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                    {
                        if (UILayer.LeftPressed)
                        {
                            AddNewShape(coord3D);
                        }
                        UILayer.ConsumeLeft();
                    }
                }
            }
        }

        private void UpdateVertexBuffer(GraphicsDevice graphicsDevice, Shape shape)
        {
            var shapeAsVertices = new List<VertexPositionColor>();
            for (int i = 0; i < shape.shape.Count; i++)
            {
                var p1 = shape.shape[i];
                var p4 = shape.shape[(i + 1) % shape.shape.Count];
                for (int j = 0; j < 10; j++)
                {
                    float t = j / 10.0f;
                    SphereVector curvePoint = p1.CurveTowards(p4, j / 10.0f);
                    SphereVector curvePoint2 = p1.CurveTowards(p4, ((j + 1) / 10.0f));
                    AddTwoLongLatLinesIfNecessary(shapeAsVertices, curvePoint, curvePoint2, Color.Red);
                }
            }
            if (shape.vertexBuffer != null) shape.vertexBuffer.Dispose();
            shape.vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, shapeAsVertices.Count, BufferUsage.WriteOnly);
            shape.vertexBuffer.SetData(shapeAsVertices.ToArray());
        }

        private void DrawPoint(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double halfSize, Vector2d v, Color color)
        {
            GraphicsBasic.DrawRect(graphicsDevice, basicEffect, v.X - halfSize, v.Y - halfSize, halfSize * 2, halfSize * 2, color);
        }
    }
}

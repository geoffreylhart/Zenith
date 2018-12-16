using System;
using System.Collections.Generic;
using System.IO;
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
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class GeometryEditor : IFlatComponent, IEditorGameComponent
    {
        private static int CURVE_SEGS = 1;
        private static bool TESSELATE = true;
        private static bool SHOW_HANDLES = false;
        private static bool SHOW_CLOUDS = false;
        private List<Shape> shapes = new List<Shape>();
        private SphereVector previewPoint;
        private int draggingPointShapeIndex = -1;
        private int draggingPointIndex = -1;
        private IndexType draggingPointIndexType = IndexType.BASE;
        private VertexBuffer landVertexBuffer = null;
        private Texture2D terrainTexture;
        private Texture2D cloudTexture;

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

            public VectorHandle()
            {
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
            LoadMap();
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

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            if (terrainTexture == null) InitTextures(graphicsDevice);
            float halfSize = (float)(0.2 * Math.Pow(0.5, cameraZoom));
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
            basicEffect.VertexColorEnabled = true;
            List<VertexPositionColor> shapeHandles = new List<VertexPositionColor>();
            if (SHOW_HANDLES)
            {
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
                shapeHandles = new List<VertexPositionColor>();
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
            }
            if (landVertexBuffer != null)
            {
                graphicsDevice.SetVertexBuffer(landVertexBuffer);
                basicEffect.TextureEnabled = true;
                basicEffect.Texture = terrainTexture;
                basicEffect.VertexColorEnabled = false;
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, landVertexBuffer.VertexCount - 2);
                }
            }
            if (!TESSELATE)
            {
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
            }
            if (SHOW_HANDLES)
            {
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
            if (SHOW_CLOUDS)
            {
                GraphicsBasic.DrawSpriteRectWeird(graphicsDevice, -Math.PI, -Math.PI / 2, Math.PI * 2, Math.PI, (minX + Math.PI) / (2 * Math.PI), (minY + Math.PI / 2) / Math.PI, (maxX - minX) / (2 * Math.PI), (maxY - minY) / Math.PI, cloudTexture);
            }
        }

        private void InitTextures(GraphicsDevice graphicsDevice)
        {
            String filePath = @"..\..\..\..\LocalCache\";
            using (var reader = File.OpenRead(filePath + "terrain.png"))
            {
                terrainTexture = Texture2D.FromStream(graphicsDevice, reader);
            }
            using (var reader = File.OpenRead(filePath + "clouds.png"))
            {
                cloudTexture = Texture2D.FromStream(graphicsDevice, reader);
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
            List<VertexPositionTexture> triangles = new List<VertexPositionTexture>();
            float z = -10f;
            var tess = new LibTessDotNet.Tess();
            foreach (var shape in shapes)
            {
                var contour = new List<LibTessDotNet.ContourVertex>();
                LongLat prevLongLat = null;
                for (int i = 0; i < shape.shape.Count; i++)
                {
                    VectorHandle p1 = shape.shape[i];
                    VectorHandle p4 = shape.shape[(i + 1) % shape.shape.Count];
                    for (int j = 0; j < CURVE_SEGS; j++)
                    {
                        float t = j / (float)CURVE_SEGS;
                        SphereVector curvePoint = p1.CurveTowards(p4, t);
                        LongLat asLongLat = curvePoint.ToLongLat();
                        if (prevLongLat != null)
                        {
                            if (Math.Abs(prevLongLat.X - asLongLat.X) > Math.PI)
                            {
                                double offsetX = asLongLat.X < prevLongLat.X ? Math.PI * 2 : -Math.PI * 2;
                                // super cheaty way to make tesselator handle some logic for us
                                var v1 = new LibTessDotNet.ContourVertex();
                                var v2 = new LibTessDotNet.ContourVertex();
                                var v3 = new LibTessDotNet.ContourVertex();
                                var v4 = new LibTessDotNet.ContourVertex();
                                v1.Position = new LibTessDotNet.Vec3 { X = (float)(asLongLat.X + offsetX), Y = (float)asLongLat.Y, Z = 0 };
                                v2.Position = new LibTessDotNet.Vec3 { X = (float)(asLongLat.X + 2 * offsetX), Y = -20, Z = 0 };
                                v3.Position = new LibTessDotNet.Vec3 { X = (float)(prevLongLat.X - 2 * offsetX), Y = -10, Z = 0 };
                                v4.Position = new LibTessDotNet.Vec3 { X = (float)(prevLongLat.X - offsetX), Y = (float)prevLongLat.Y, Z = 0 };
                                contour.Add(v1);
                                contour.Add(v2);
                                contour.Add(v3);
                                contour.Add(v4);
                            }
                        }
                        prevLongLat = asLongLat;
                        LibTessDotNet.ContourVertex newVert = new LibTessDotNet.ContourVertex();
                        newVert.Position = new LibTessDotNet.Vec3 { X = (float)asLongLat.X, Y = (float)asLongLat.Y, Z = 0 };
                        contour.Add(newVert);
                    }
                }
                tess.AddContour(contour.ToArray(), LibTessDotNet.ContourOrientation.Original);
            }
            tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);
            for (int i = 0; i < tess.ElementCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var pos = tess.Vertices[tess.Elements[i * 3 + j]].Position;
                    // TODO: why 1-y?
                    triangles.Add(new VertexPositionTexture(new Vector3(pos.X, pos.Y, z), new Vector2((float)((pos.X + Math.PI) / (2 * Math.PI)), (float)(1 - (pos.Y + Math.PI / 2) / Math.PI))));
                }
            }
            if (landVertexBuffer != null) landVertexBuffer.Dispose();
            landVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, triangles.Count, BufferUsage.WriteOnly);
            landVertexBuffer.SetData(triangles.ToArray());
        }

        public void Update(double mouseX, double mouseY, double cameraZoom)
        {
            if (!SHOW_HANDLES) return;
            if (!UILayer.LeftDown)
            {
                draggingPointIndex = -1;
                draggingPointShapeIndex = -1;
            }
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
                    draggingPointShapeIndex = bestShapeIndex;
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
                    var diff = coord3D - shapes[draggingPointShapeIndex].shape[draggingPointIndex].p;
                    shapes[draggingPointShapeIndex].shape[draggingPointIndex].p = coord3D;
                    shapes[draggingPointShapeIndex].shape[draggingPointIndex].incoming = new SphereVector((shapes[draggingPointShapeIndex].shape[draggingPointIndex].incoming + diff).Normalized());
                    shapes[draggingPointShapeIndex].shape[draggingPointIndex].outgoing = new SphereVector((shapes[draggingPointShapeIndex].shape[draggingPointIndex].outgoing + diff).Normalized());
                }
                if (draggingPointIndexType == IndexType.INCOMING)
                {
                    shapes[draggingPointShapeIndex].shape[draggingPointIndex].incoming = coord3D;
                    if (shapes[draggingPointShapeIndex].shape[draggingPointIndex].handleType == HandleType.SMOOTH)
                    {
                        shapes[draggingPointShapeIndex].shape[draggingPointIndex].outgoing = shapes[draggingPointShapeIndex].shape[draggingPointIndex].p.WalkTowards(coord3D, -shapes[draggingPointShapeIndex].shape[draggingPointIndex].p.Distance(shapes[draggingPointShapeIndex].shape[draggingPointIndex].outgoing));
                    }
                }
                if (draggingPointIndexType == IndexType.OUTGOING)
                {
                    shapes[draggingPointShapeIndex].shape[draggingPointIndex].outgoing = coord3D;
                    if (shapes[draggingPointShapeIndex].shape[draggingPointIndex].handleType == HandleType.SMOOTH)
                    {
                        shapes[draggingPointShapeIndex].shape[draggingPointIndex].incoming = shapes[draggingPointShapeIndex].shape[draggingPointIndex].p.WalkTowards(coord3D, -shapes[draggingPointShapeIndex].shape[draggingPointIndex].p.Distance(shapes[draggingPointShapeIndex].shape[draggingPointIndex].incoming));
                    }
                }
                shapes[draggingPointShapeIndex].updateVertexBuffer = true;
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
                        draggingPointShapeIndex = bestShapeIndex;
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
                for (int j = 0; j < CURVE_SEGS; j++)
                {
                    float t = j / (float)CURVE_SEGS;
                    SphereVector curvePoint = p1.CurveTowards(p4, j / (float)CURVE_SEGS);
                    SphereVector curvePoint2 = p1.CurveTowards(p4, ((j + 1) / (float)CURVE_SEGS));
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

        public List<string> GetDebugInfo()
        {
            return new List<string>();
        }

        public List<IUIComponent> GetSettings()
        {
            List<IUIComponent> components = new List<IUIComponent>();
            components.Add(new Button("Save Map") { OnClick = SaveMap });
            return components;
        }

        private static string MAP_PATH = @"..\..\..\..\Data\EARTH";
        private void LoadMap()
        {
            if (!File.Exists(MAP_PATH))
            {
                shapes = new List<Shape>();
                // TODO: get rid of those code after conversion? or hide it away
                using (var reader = new StreamReader(@"..\..\..\..\GraphicsSource\InkScape\continentsWithAntarcticaLow.svg"))
                {
                    String window = "";
                    while (!reader.EndOfStream)
                    {
                        int asInt = reader.Read();
                        window += (char)asInt;
                        if (window.Length > 10) window = window.Substring(1, 10);
                        if (window.EndsWith(" d="))
                        {
                            ReadShape(reader);
                        }
                    }
                }
                return;
            }
            shapes = new List<Shape>();
            using (var reader = new BinaryReader(new FileStream(MAP_PATH, FileMode.Open)))
            {
                int shapeCount = reader.ReadInt32();
                for (int i = 0; i < shapeCount; i++)
                {
                    var newShape = new Shape();
                    newShape.shape = new List<VectorHandle>();
                    int handleCount = reader.ReadInt32();
                    for (int j = 0; j < handleCount; j++)
                    {
                        var newHandle = new VectorHandle();
                        newHandle.handleType = (HandleType)reader.ReadInt32();
                        newHandle.incoming = new SphereVector(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
                        newHandle.outgoing = new SphereVector(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
                        newHandle.p = new SphereVector(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
                        newShape.shape.Add(newHandle);
                    }
                    shapes.Add(newShape);
                }
            }
        }

        static double minX = 10000;
        static double maxX = -10000;
        static double minY = 10000;
        static double maxY = -10000;
        private void ReadShape(StreamReader reader)
        {
            reader.Read(); // skip first quote
            var newShape = new Shape();
            newShape.shape = new List<VectorHandle>();
            String token = "";
            double oldX = -1;
            double oldY = -1;
            while (true)
            {
                char asChar = (char)reader.Read();
                if (asChar == '\"') break;
                if (token.Equals("") && "MLHVmlhv".Contains(asChar))
                {
                    token += asChar;
                    continue;
                }
                if ("MLHVmlhvz".Contains(asChar))
                {
                    double[] parsed = token.Substring(1).Split(',').Select(z => double.Parse(z)).ToArray();
                    double x, y;
                    switch (token[0])
                    {
                        case 'M':
                        case 'L':
                            x = parsed[0];
                            y = parsed[1];
                            break;
                        case 'H':
                            x = parsed[0];
                            y = 0;
                            break;
                        case 'V':
                            x = oldX; // TODO: why doesn't this seem to match the specification?
                            y = parsed[0];
                            break;
                        case 'm':
                        case 'l':
                            x = parsed[0] + oldX;
                            y = parsed[1] + oldY;
                            break;
                        case 'h':
                            x = parsed[0] + oldX;
                            y = oldY;
                            break;
                        case 'v':
                            x = oldX;
                            y = parsed[0] + oldY;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    oldX = x;
                    oldY = y;
                    minX = Math.Min(x, minX);
                    maxX = Math.Max(x, maxX);
                    minY = Math.Min(y, minY);
                    maxY = Math.Max(y, maxY);
                    var newHandle = new VectorHandle();
                    newHandle.handleType = HandleType.SHARP;
                    // high
                    //double xStart = -0.050;
                    //double yStart = -1124.050;
                    //double w = 1010.430;
                    //double h = 1224.100;
                    //double minLong = -169.516691 * Math.PI / 180;
                    //double maxLat = 83.600842 * Math.PI / 180;
                    //double maxLong = 190.519684 * Math.PI / 180;
                    //double minLat = -89.000389 * Math.PI / 180;
                    // low
                    double xStart = 0;
                    double yStart = 0;
                    double w = 1024.71;
                    // TODO: figure out why inkscape disagrees with me on the proportion of things
                    w = 1009.890; // acording to inkscape
                    double h = 1224;
                    h = 1224.100; // according to inkscape
                    double minLong = -169.522279 * Math.PI / 180;
                    double maxLat = 83.646363 * Math.PI / 180;
                    double maxLong = 190.518061 * Math.PI / 180;
                    double minLat = -89.000292 * Math.PI / 180;
                    double longVal = (x - xStart) / w * (maxLong - minLong) + minLong;
                    // TODO: figure out why its max-x instead of x+min
                    double texY = ToY(maxLat) - (y - yStart) / h * (ToY(maxLat) - ToY(minLat));
                    double latVal = ToLat(texY);
                    LongLat longLat = new LongLat(longVal, latVal);
                    newHandle.p = longLat.ToSphereVector();
                    newHandle.incoming = newHandle.p;
                    newHandle.outgoing = newHandle.p;
                    newShape.shape.Add(newHandle);
                    if (asChar == 'z')
                    {
                        shapes.Add(newShape);
                        newShape = new Shape();
                        newShape.shape = new List<VectorHandle>();
                    }
                    token = "";
                    if ("MLHVmlhv".Contains(asChar)) token += asChar;
                }
                else
                {
                    token += asChar;
                }
            }
        }
        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }
        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }

        private void SaveMap()
        {
            // overwrites
            using (var writer = new BinaryWriter(new FileStream(MAP_PATH, FileMode.Create)))
            {
                writer.Write(shapes.Count);
                foreach (var shape in shapes)
                {
                    writer.Write(shape.shape.Count);
                    foreach (var handle in shape.shape)
                    {
                        writer.Write((int)handle.handleType);
                        writer.Write(handle.incoming.X);
                        writer.Write(handle.incoming.Y);
                        writer.Write(handle.incoming.Z);
                        writer.Write(handle.outgoing.X);
                        writer.Write(handle.outgoing.Y);
                        writer.Write(handle.outgoing.Z);
                        writer.Write(handle.p.X);
                        writer.Write(handle.p.Y);
                        writer.Write(handle.p.Z);
                    }
                }
            }
        }

        public List<IEditorGameComponent> GetSubComponents()
        {
            return new List<IEditorGameComponent>();
        }
    }
}

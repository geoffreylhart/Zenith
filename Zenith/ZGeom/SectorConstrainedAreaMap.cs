using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibTessDotNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TriangleNet.Geometry;
using Zenith.LibraryWrappers;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    public class SectorConstrainedAreaMap
    {
        public List<List<Vector2d>> paths = new List<List<Vector2d>>();
        public List<List<Vector2d>> inners = new List<List<Vector2d>>(); // holes
        public List<List<Vector2d>> outers = new List<List<Vector2d>>(); // islands

        internal BasicVertexBuffer ConstructAsRoads(GraphicsDevice graphicsDevice, double width, Texture2D texture, Color color)
        {
            LineGraph lineGraph = new LineGraph();
            foreach (var path in paths)
            {
                LineGraph.GraphNode prev = null;
                for (int i = 0; i < path.Count; i++)
                {
                    var next = new LineGraph.GraphNode(path[i]);
                    if (prev != null)
                    {
                        prev.nextConnections.Add(next);
                        next.prevConnections.Add(prev);
                    }
                    lineGraph.nodes.Add(next);
                    prev = next;
                }
            }
            foreach (var path in inners)
            {
                LineGraph.GraphNode prev = null;
                LineGraph.GraphNode first = null;
                LineGraph.GraphNode last = null;
                for (int i = 0; i < path.Count - 1; i++) // since our loops end in a duplicate
                {
                    var next = new LineGraph.GraphNode(path[i]);
                    if (prev != null)
                    {
                        prev.nextConnections.Add(next);
                        next.prevConnections.Add(prev);
                    }
                    lineGraph.nodes.Add(next);
                    last = next;
                    if (first == null) first = next;
                    prev = next;
                }
                first.prevConnections.Add(last);
                last.nextConnections.Add(first);
            }
            foreach (var path in outers)
            {
                LineGraph.GraphNode prev = null;
                LineGraph.GraphNode first = null;
                LineGraph.GraphNode last = null;
                for (int i = 0; i < path.Count - 1; i++) // since our loops end in a duplicate
                {
                    var next = new LineGraph.GraphNode(path[i]);
                    if (prev != null)
                    {
                        prev.nextConnections.Add(next);
                        next.prevConnections.Add(prev);
                    }
                    lineGraph.nodes.Add(next);
                    last = next;
                    if (first == null) first = next;
                    prev = next;
                }
                first.prevConnections.Add(last);
                last.nextConnections.Add(first);
            }
            return lineGraph.ConstructAsRoads(graphicsDevice, width, texture, color);
        }

        internal BasicVertexBuffer Tesselate(GraphicsDevice graphicsDevice, Color color, bool cornersAreFilled)
        {
            return new BasicVertexBuffer(graphicsDevice, GetTesselationVertices(color, cornersAreFilled), PrimitiveType.TriangleList);
        }

        public List<VertexPositionColor> GetTesselationVertices(Color color, bool cornersAreFilled)
        {
            var fakeSector = new CubeSector(CubeSector.CubeSectorFace.FRONT, 0, 0, 8);
            List<List<ContourVertex>> contours = paths.Select(x => x.Select(y => Vector2DToContourVertex(y)).ToList()).ToList();
            contours = OSMPolygonBufferGenerator.CloseLines(fakeSector, contours);
            foreach (var inner in inners)
            {
                var innerContours = new List<List<ContourVertex>>() { inner.Skip(1).Select(x => Vector2DToContourVertex(x, true)).ToList() }; // skip 1 since our loops end in a duplicate
                contours.AddRange(innerContours);
            }
            foreach (var outer in outers)
            {
                var outerContours = new List<List<ContourVertex>>() { outer.Skip(1).Select(y => Vector2DToContourVertex(y)).ToList() }; // skip 1 since our loops end in a duplicate
                contours.AddRange(outerContours);
            }
            if (paths.Count == 0 && cornersAreFilled)
            {
                // add one outer surrounding the hole sector
                var square = new List<ContourVertex>();
                square.Add(new ContourVertex() { Position = new Vec3() { X = 0, Y = 0, Z = 0 } });
                square.Add(new ContourVertex() { Position = new Vec3() { X = 0, Y = 1, Z = 0 } });
                square.Add(new ContourVertex() { Position = new Vec3() { X = 1, Y = 1, Z = 0 } });
                square.Add(new ContourVertex() { Position = new Vec3() { X = 1, Y = 0, Z = 0 } });
                contours.Add(square);
            }
            var vertices = OSMPolygonBufferGenerator.Tesselate(contours, color);
            return vertices;
        }

        private ContourVertex Vector2DToContourVertex(Vector2d v, bool isHole = false)
        {
            return new ContourVertex() { Position = new Vec3() { X = (float)v.X, Y = (float)v.Y, Z = 0 }, Data = isHole };
        }
    }
}

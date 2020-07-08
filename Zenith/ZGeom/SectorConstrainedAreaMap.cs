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
        public List<List<Vector2d>> inners = new List<List<Vector2d>>(); // holes
        public List<List<Vector2d>> outers = new List<List<Vector2d>>(); // islands

        internal BasicVertexBuffer ConstructAsRoads(GraphicsDevice graphicsDevice, double width, Texture2D texture, Color color)
        {
            LineGraph lineGraph = new LineGraph();
            AddLoopsToLineGraph(lineGraph, inners);
            AddLoopsToLineGraph(lineGraph, outers);
            return lineGraph.ConstructAsRoads(graphicsDevice, width, texture, color);
        }

        private void AddLoopsToLineGraph(LineGraph lineGraph, List<List<Vector2d>> inners)
        {
            foreach (var path in inners)
            {
                var newNodes = new LineGraph.GraphNode[path.Count - 1];
                for (int i = 0; i < path.Count - 1; i++) // since our loops end in a duplicate
                {
                    newNodes[i] = new LineGraph.GraphNode(path[i]);
                    lineGraph.nodes.Add(newNodes[i]);
                }
                for (int i = 0; i < newNodes.Length; i++)
                {
                    var prev = newNodes[i];
                    var next = newNodes[(i + 1) % newNodes.Length];
                    // TODO: don't assume all of these are border intersections - some lines legitimately lie along the border and are perfectly horizontal/straight - I think?
                    if (!(prev.pos.X == 0 && next.pos.X == 0 || prev.pos.X == 1 && next.pos.X == 1 || prev.pos.Y == 0 && next.pos.Y == 0 || prev.pos.Y == 1 && next.pos.Y == 1))
                    {
                        prev.nextConnections.Add(next);
                        next.prevConnections.Add(prev);
                    }
                }
            }
        }

        internal BasicVertexBuffer Tesselate(GraphicsDevice graphicsDevice, Color color)
        {
            return new BasicVertexBuffer(graphicsDevice, GetTesselationVertices(color), PrimitiveType.TriangleList);
        }

        public List<VertexPositionColor> GetTesselationVertices(Color color)
        {
            var fakeSector = new CubeSector(CubeSector.CubeSectorFace.FRONT, 0, 0, 8);
            List<List<ContourVertex>> contours = new List<List<ContourVertex>>();
            foreach (var inner in inners)
            {
                var innerContour = inner.Skip(1).Select(x => Vector2DToContourVertex(x, true)).ToList(); // skip 1 since our loops end in a duplicate
                if (innerContour.Count == 3)
                {
                    // TODO: the tesselator just doesn't like triangles!? seriously??
                    var avg01 = new ContourVertex() { Position = new Vec3() { X = innerContour[0].Position.X / 2 + innerContour[1].Position.X / 2, Y = innerContour[0].Position.Y / 2 + innerContour[1].Position.Y / 2, Z = 0 }, Data = innerContour[0].Data };
                    innerContour.Insert(1, avg01);
                }
                contours.Add(innerContour);
            }
            foreach (var outer in outers)
            {
                var outerContours = new List<List<ContourVertex>>() { outer.Skip(1).Select(y => Vector2DToContourVertex(y)).ToList() }; // skip 1 since our loops end in a duplicate
                contours.AddRange(outerContours);
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

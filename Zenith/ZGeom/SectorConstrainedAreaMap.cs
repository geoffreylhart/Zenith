﻿using System;
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
    class SectorConstrainedAreaMap
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

        internal BasicVertexBuffer Tesselate(GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Color color)
        {
            var fakeSector = new CubeSector(CubeSector.CubeSectorFace.FRONT, 0, 0, 8);
            List<List<ContourVertex>> contours = paths.Select(x => x.Select(Vector2DToContourVertex).ToList()).ToList();
            var vertices = OSMPolygonBufferGenerator.Tesselate(OSMPolygonBufferGenerator.CloseLines(fakeSector, contours), color);
            foreach (var outer in outers)
            {
                var outerContours = new List<List<ContourVertex>>() { outer.Skip(1).Select(Vector2DToContourVertex).ToList() }; // skip 1 since our loops end in a duplicate
                vertices.AddRange(OSMPolygonBufferGenerator.Tesselate(outerContours, color));
            }
            return new BasicVertexBuffer(graphicsDevice, vertices, PrimitiveType.TriangleList);
        }

        private ContourVertex Vector2DToContourVertex(Vector2d v)
        {
            return new ContourVertex() { Position = new Vec3() { X = (float)v.X, Y = (float)v.Y, Z = 0 } };
        }
    }
}
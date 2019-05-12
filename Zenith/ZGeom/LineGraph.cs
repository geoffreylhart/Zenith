﻿using LibTessDotNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    internal class LineGraph : IGeom
    {
        internal List<GraphNode> nodes = new List<GraphNode>();

        public BasicVertexBuffer Construct(GraphicsDevice graphicsDevice)
        {
            List<int> indices = new List<int>();
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            Dictionary<GraphNode, int> indexLookup = new Dictionary<GraphNode, int>();
            for (int i = 0; i < nodes.Count; i++) indexLookup[nodes[i]] = i;
            foreach (var node in nodes)
            {
                vertices.Add(new VertexPositionColor(new Vector3(node.pos, -10f), Color.White));
                foreach (var c in node.nextConnections)
                {
                    int i1 = indexLookup[node];
                    int i2 = indexLookup[c];
                    if (i1 < i2)
                    {
                        indices.Add(i1);
                        indices.Add(i2);
                    }
                }
            }
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, PrimitiveType.LineList);
        }

        internal BasicVertexBuffer ConstructAsRoads(GraphicsDevice graphicsDevice, double width, Texture2D texture, Color color)
        {
            List<int> indices = new List<int>();
            List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
            Dictionary<GraphNode, int> indexLookup = new Dictionary<GraphNode, int>();
            for (int i = 0; i < nodes.Count; i++) indexLookup[nodes[i]] = i;
            foreach (var node in nodes)
            {
                foreach (var c in node.nextConnections)
                {
                    Vector2d v1 = node.pos;
                    Vector2d v2 = c.pos;
                    Vector2d w = (v2 - v1).RotateCW90().Normalized() * width / 2; // points right
                    Vector2d topLeft = v2 - w;
                    Vector2d topRight = v2 + w;
                    Vector2d bottomLeft = v1 - w;
                    Vector2d bottomRight = v1 + w;
                    int i = vertices.Count;
                    vertices.Add(new VertexPositionTexture(new Vector3(topLeft, -10f), new Vector2(0, 0)));
                    vertices.Add(new VertexPositionTexture(new Vector3(topRight, -10f), new Vector2(1, 0)));
                    vertices.Add(new VertexPositionTexture(new Vector3(bottomRight, -10f), new Vector2(1, 1)));
                    vertices.Add(new VertexPositionTexture(new Vector3(bottomLeft, -10f), new Vector2(0, 1)));
                    indices.Add(i);
                    indices.Add(i + 2); // TODO: why was flipping opposite that I expect correct?
                    indices.Add(i + 1);
                    indices.Add(i);
                    indices.Add(i + 3);
                    indices.Add(i + 2);
                }
            }
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, texture, PrimitiveType.TriangleList);
        }

        internal LineGraph Combine(LineGraph x)
        {
            nodes.AddRange(x.nodes);
            return this;
        }

        internal List<List<ContourVertex>> ToContours()
        {
            List<List<ContourVertex>> contours = new List<List<ContourVertex>>();
            List<GraphNode> starts = nodes.Where(x => x.prevConnections.Count == 0 && x.nextConnections.Count == 1).ToList();
            HashSet<GraphNode> visited = new HashSet<GraphNode>();
            foreach (var start in starts)
            {
                List<ContourVertex> contour = new List<ContourVertex>();
                GraphNode next = start;
                GraphNode prev = null;
                while (next != null)
                {
                    ContourVertex vertex = new ContourVertex();
                    vertex.Position = new Vec3 { X = (float)next.pos.X, Y = (float)next.pos.Y, Z = 0 };
                    contour.Add(vertex);
                    visited.Add(next);
                    GraphNode nextnext = GetNext(prev, next);
                    prev = next;
                    next = nextnext;
                }
                if (contour.Count > 0)
                {
                    contours.Add(contour);
                }
            }
            // now find the loops
            foreach (var node in nodes)
            {
                List<ContourVertex> contour = new List<ContourVertex>();
                GraphNode next = node;
                GraphNode prev = null;
                while (!visited.Contains(next))
                {
                    ContourVertex vertex = new ContourVertex();
                    vertex.Data = next.isHole; // TODO: get rid of this
                    vertex.Position = new Vec3 { X = (float)next.pos.X, Y = (float)next.pos.Y, Z = 0 };
                    contour.Add(vertex);
                    visited.Add(next);
                    GraphNode nextnext = GetNext(prev, next);
                    prev = next;
                    next = nextnext;
                }
                if (contour.Count > 0)
                {
                    contour.Add(contour[0]); // close it?
                    contours.Add(contour);
                }
            }
            return contours;
        }

        private GraphNode GetNext(GraphNode prev, GraphNode node)
        {
            // note: sometimes our connections are going the opposite direction that we expect
            // this is thanks to multipolyons sharing edges with the coastline, as an example (it can only travel the correct direction for one thing)
            int totalCount = node.prevConnections.Count + node.nextConnections.Count;
            if (totalCount == 1 && prev == null)
            {
                if (node.prevConnections.Count == 1) return node.prevConnections[0];
                return node.nextConnections[0];
            }
            if (totalCount == 1) return null;
            if (totalCount != 2) throw new NotImplementedException();
            List<GraphNode> combined = new List<GraphNode>();
            combined.AddRange(node.prevConnections);
            combined.AddRange(node.nextConnections);
            if (prev == null) return combined[1];
            return combined.Where(x => x != prev).Single();
        }

        internal class GraphNode
        {
            internal Vector2d pos;
            internal List<GraphNode> nextConnections = new List<GraphNode>();
            internal List<GraphNode> prevConnections = new List<GraphNode>();
            internal bool isHole = false; // TODO: get rid of this

            public GraphNode(Vector2d pos)
            {
                this.pos = pos;
            }
        }
    }
}

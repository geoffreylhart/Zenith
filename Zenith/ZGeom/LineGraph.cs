using LibTessDotNet;
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

        internal List<List<ContourVertex>> ToContours()
        {
            List<List<ContourVertex>> contours = new List<List<ContourVertex>>();
            List<GraphNode> starts = nodes.Where(x => x.prevConnections.Count == 0).ToList();
            HashSet<GraphNode> visited = new HashSet<GraphNode>();
            foreach (var start in starts)
            {
                List<ContourVertex> contour = new List<ContourVertex>();
                GraphNode next = start;
                while (true)
                {
                    ContourVertex vertex = new ContourVertex();
                    vertex.Position = new Vec3 { X = (float)next.pos.X, Y = (float)next.pos.Y, Z = 0 };
                    contour.Add(vertex);
                    visited.Add(next);
                    if (next.nextConnections.Count == 0) break;
                    next = next.nextConnections.Single();
                }
                if (contour.Count > 0)
                {
                    contours.Add(contour);
                }
            }
            // now find the loops
            foreach(var node in nodes)
            {
                List<ContourVertex> contour = new List<ContourVertex>();
                GraphNode next = node;
                while (!visited.Contains(next))
                {
                    ContourVertex vertex = new ContourVertex();
                    vertex.Position = new Vec3 { X = (float)next.pos.X, Y = (float)next.pos.Y, Z = 0 };
                    contour.Add(vertex);
                    visited.Add(next);
                    next = next.nextConnections.Single();
                }
                if (contour.Count > 0)
                {
                    contour.Add(contour[0]); // close it?
                    contours.Add(contour);
                }
            }
            return contours;
        }

        internal class GraphNode
        {
            internal Vector2d pos;
            internal List<GraphNode> nextConnections = new List<GraphNode>();
            internal List<GraphNode> prevConnections = new List<GraphNode>();

            public GraphNode(Vector2d pos)
            {
                this.pos = pos;
            }
        }
    }
}

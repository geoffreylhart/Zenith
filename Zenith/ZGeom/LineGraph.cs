using GeoAPI.Geometries;
using LibTessDotNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using Zenith.MathHelpers;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    internal class LineGraph : IGeom
    {
        static float RIDGE_HEIGHT = 0.00001f;
        internal List<GraphNode> nodes = new List<GraphNode>();

        public BasicVertexBuffer Construct(GraphicsDevice graphicsDevice)
        {
            List<int> indices = new List<int>();
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            Dictionary<GraphNode, int> indexLookup = new Dictionary<GraphNode, int>();
            for (int i = 0; i < nodes.Count; i++) indexLookup[nodes[i]] = i;
            foreach (var node in nodes)
            {
                vertices.Add(new VertexPositionColor(new Vector3(node.pos, 0), Color.White));
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
        internal BasicVertexBuffer ConstructAsRoads(GraphicsDevice graphicsDevice, double width, Texture2D texture, Color color, bool outerOnly)
        {
            if (outerOnly)
            {
                return ConstructViaExtrusion(graphicsDevice, new[] { new Vector2d(-width, -RIDGE_HEIGHT), new Vector2d(0, 0) }, texture, color);
            }
            else
            {
                return ConstructViaExtrusion(graphicsDevice, new[] { new Vector2d(-width, 0), new Vector2d(0, RIDGE_HEIGHT), new Vector2d(width, 0) }, texture, color);
            }
        }

        internal BasicVertexBuffer ConstructViaExtrusion(GraphicsDevice graphicsDevice, Vector2d[] shape, Texture2D texture, Color color)
        {
            List<int> indices = new List<int>();
            List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
            Dictionary<GraphNode, int> indexLookup = new Dictionary<GraphNode, int>();
            for (int i = 0; i < nodes.Count; i++) indexLookup[nodes[i]] = i;
            foreach (var prev in nodes)
            {
                foreach (var next in prev.nextConnections)
                {
                    var prevPrevs = new List<GraphNode>();
                    prevPrevs.AddRange(prev.prevConnections);
                    prevPrevs.AddRange(prev.nextConnections);
                    prevPrevs = prevPrevs.Where(x => x != next).ToList();
                    var nextNexts = new List<GraphNode>();
                    nextNexts.AddRange(next.prevConnections);
                    nextNexts.AddRange(next.nextConnections);
                    nextNexts = nextNexts.Where(x => x != prev).ToList();

                    // sort clockwise around the line
                    prevPrevs = prevPrevs.OrderBy(x => ComputeInnerAngle(prev.pos - next.pos, x.pos - prev.pos)).ToList();
                    nextNexts = nextNexts.OrderBy(x => ComputeInnerAngle(next.pos - prev.pos, x.pos - next.pos)).ToList();
                    Vector2d v1 = prev.pos;
                    Vector2d v2 = next.pos;
                    int i = vertices.Count;
                    double[] sumLengths = new double[shape.Length];
                    for (int j = 1; j < shape.Length; j++)
                    {
                        sumLengths[j] = sumLengths[j - 1] + (shape[j] - shape[j - 1]).Length();
                    }
                    double totalLength = sumLengths.Last();
                    for (int j = 0; j < shape.Length; j++)
                    {
                        sumLengths[j] /= totalLength;
                    }
                    double texLength = (v2 - v1).Length() / totalLength;
                    for (int j = 0; j < shape.Length; j++)
                    {
                        var v = shape[j];
                        Vector2d topW, bottomW;
                        double topTexOffset, bottomTexOffset;
                        if (v.X == 0)
                        {
                            topW = new Vector2d(0, 0);
                            bottomW = new Vector2d(0, 0);
                            topTexOffset = 0;
                            bottomTexOffset = 0;
                        }
                        else
                        {
                            bool isLeft = v.X < 0;
                            topW = GetW(prev, next, nextNexts.Count == 0 ? null : (isLeft ? nextNexts.Last() : nextNexts.First()), -v.X);
                            bottomW = GetW(next, prev, prevPrevs.Count == 0 ? null : (isLeft ? prevPrevs.First() : prevPrevs.Last()), v.X);
                            topTexOffset = Vector2d.Dot(topW, v2 - v1) / (v2 - v1).Length() / totalLength * (isLeft ? -1 : 1);
                            bottomTexOffset = -Vector2d.Dot(bottomW, v2 - v1) / (v2 - v1).Length() / totalLength * (isLeft ? -1 : 1);
                        }
                        Vector2d top = v2 + topW;
                        Vector2d bottom = v1 + bottomW;
                        // TODO: these are guesses - they would certainly work without the "stretching" I do, but I'm not sure otherwise
                        vertices.Add(new VertexPositionTexture(new Vector3(top, (float)v.Y), new Vector2((float)(1 - sumLengths[j]), (float)topTexOffset)));
                        vertices.Add(new VertexPositionTexture(new Vector3(bottom, (float)v.Y), new Vector2((float)(1 - sumLengths[j]), (float)texLength + (float)bottomTexOffset)));
                        if (j > 0)
                        {
                            // topleft, bottomrright, topright + topleft, bottom, bottomright
                            indices.Add(i + (j - 1) * 2);
                            indices.Add(i + 3 + (j - 1) * 2);
                            indices.Add(i + 2 + (j - 1) * 2);
                            indices.Add(i + (j - 1) * 2);
                            indices.Add(i + 1 + (j - 1) * 2);
                            indices.Add(i + 3 + (j - 1) * 2);
                        }
                    }
                }
            }
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, texture, true, PrimitiveType.TriangleList);
        }

        internal LineGraph ForceDirection(bool ccw)
        {
            ccw = !ccw; // TODO: fix this to not be hacky
            // copy pasted from contour generation code
            HashSet<GraphNode> visited = new HashSet<GraphNode>();
            foreach (var node in nodes)
            {
                List<GraphNode> contour = new List<GraphNode>();
                GraphNode next = node;
                GraphNode prev = null;
                while (!visited.Contains(next))
                {
                    contour.Add(next);
                    visited.Add(next);
                    GraphNode nextnext = GetNext(prev, next);
                    if (nextnext == null) break;
                    prev = next;
                    next = nextnext;
                }
                if (contour.Count > 2)
                {
                    contour.Add(contour[0]); // close it?
                    // found a loop
                    var coords = contour.Select(x => new Coordinate(x.pos.X, x.pos.Y)).ToArray();
                    var ring = new LinearRing(coords);
                    if (ring.IsCCW != ccw)
                    {
                        for (int i = 0; i < contour.Count - 1; i++)
                        {
                            GraphNode before = contour[(i - 1 + contour.Count - 1) % (contour.Count - 1)];
                            GraphNode after = contour[i + 1];
                            contour[i].nextConnections = new List<GraphNode>() { before };
                            contour[i].prevConnections = new List<GraphNode>() { after };
                        }
                    }
                    else
                    {
                        for (int i = 0; i < contour.Count - 1; i++)
                        {
                            GraphNode before = contour[(i - 1 + contour.Count - 1) % (contour.Count - 1)];
                            GraphNode after = contour[i + 1];
                            contour[i].nextConnections = new List<GraphNode>() { after };
                            contour[i].prevConnections = new List<GraphNode>() { before };
                        }
                    }
                }
            }
            return this;
        }

        internal List<Matrix> ConstructHousePositions()
        {
            var matrices = new List<Matrix>();

            double houseSpacing = 0.001;
            double width = 0.0005;
            foreach (var node in nodes)
            {
                foreach (var c in node.nextConnections)
                {
                    Vector2d v1 = node.pos;
                    Vector2d v2 = c.pos;
                    Vector2d w1 = node.GetW(c, width);
                    Vector2d w2 = c.GetW(node, width);
                    Vector2d topLeft = v2 - w2;
                    Vector2d topRight = v2 + w2;
                    Vector2d bottomLeft = v1 - w1;
                    Vector2d bottomRight = v1 + w1;
                    int i = matrices.Count;
                    int houseCount = (int)((v2 - v1).Length() / houseSpacing);
                    Matrix rotation = Matrix.CreateRotationZ((float)Math.Atan2(v2.Y - v1.Y, v2.X - v1.X) + Mathf.PI);
                    Matrix rotation2 = Matrix.CreateRotationZ((float)Math.Atan2(v2.Y - v1.Y, v2.X - v1.X));
                    for (int j = 0; j < houseCount; j++)
                    {
                        double t = (j + 0.5) / houseCount;
                        var pos = new Vector3(v1 * (1 - t) + v2 * t, 0);
                        var wt = new Vector3(w1 * (1 - t) + w2 * t, 0);
                        matrices.Add(rotation * Matrix.CreateTranslation(pos + wt));
                        matrices.Add(rotation2 * Matrix.CreateTranslation(pos - wt));
                    }
                }
            }
            return matrices;
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
                    if (nextnext == null) break;
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
            if (totalCount != 2) return null;
            List<GraphNode> combined = new List<GraphNode>();
            combined.AddRange(node.prevConnections);
            combined.AddRange(node.nextConnections);
            if (prev == null) return combined[1];
            combined = combined.Where(x => !x.Equals(prev)).ToList();
            if (combined.Count != 1) return null;
            return combined.Single();
        }

        // just connect all closest pairs of points, sure
        // were doing this to put off proper closing of partially loaded multilakes
        internal LineGraph ClosePolygonNaively()
        {
            List<GraphNode> ends = nodes.Where(x => x.nextConnections.Count + x.prevConnections.Count == 1).ToList();
            if (ends.Count % 2 == 1) throw new NotImplementedException();
            while (ends.Count > 0)
            {
                GraphNode node1 = ends[0];
                GraphNode node2 = ends.OrderBy(x => (x.pos - node1.pos).Length()).ToList()[1];
                ends.Remove(node1);
                ends.Remove(node2);
                node1.nextConnections.Add(node2);
                node2.prevConnections.Add(node1);
            }
            return this;
        }

        // TODO: copy-pasted from sectorconstrainedosmareagraph
        private static double ComputeInnerAngle(Vector2d v1, Vector2d v2)
        {
            // ex: we'd expect a square island to have 4 inner angles of pi/2
            // ex: we'd expect a square pond to have 4 inner angles of 3pi/2
            // we're returning this according to our unusual coordinate system
            double cos = (v1.X * v2.X + v1.Y * v2.Y) / v1.Length() / v2.Length();
            double sin = (v2.X * v1.Y - v2.Y * v1.X) / v1.Length() / v2.Length();
            return Math.Atan2(-sin, cos) + Math.PI;
        }

        // TODO: somehow deal with very tight corners, and that one glitch which I assume it from small, sharp angles
        private Vector2d GetW(GraphNode n1, GraphNode n2, GraphNode n3, double width)
        {
            // adjust width based on longLat
            double stretch = Math.Cos(n2.pos.Y); // TODO: I don't think this stretch amount is accurate anymore
            Vector2d v1 = new Vector2d(n1.pos.X, n1.pos.Y / stretch);
            Vector2d v2 = new Vector2d(n2.pos.X, n2.pos.Y / stretch);
            Vector2d v3 = n3 == null ? null : new Vector2d(n3.pos.X, n3.pos.Y / stretch);
            Vector2d straightW = (v1 - v2).RotateCW90().Normalized() * width;
            Vector2d w; // points right
            if (v3 == null)
            {
                w = straightW;
            }
            else
            {
                Vector2d w1 = (v3 - v2).RotateCCW90().Normalized() * width;
                Vector2d w2 = (v2 - v1).RotateCCW90().Normalized() * width;
                w = (w1 + w2).Normalized() * width;
            }
            w /= Vector2d.Dot(w.Normalized(), straightW.Normalized());
            w.Y *= stretch;
            return w;
        }

        internal class GraphNode
        {
            internal Vector2d pos;
            internal List<GraphNode> nextConnections = new List<GraphNode>();
            internal List<GraphNode> prevConnections = new List<GraphNode>();
            internal List<Dictionary<string, string>> nextProps = new List<Dictionary<string, string>>(); // TODO: is this really the best way to pass on the key/values?
            internal List<Dictionary<string, string>> prevProps = new List<Dictionary<string, string>>();
            internal bool isHole = false; // TODO: get rid of this

            public GraphNode(Vector2d pos)
            {
                this.pos = pos;
            }

            internal Vector2d GetW(GraphNode target, double width)
            {
                // TODO: do all of this better
                GraphNode prev = prevConnections.Count == 1 ? prevConnections[0] : null;
                GraphNode next = nextConnections.Count == 1 ? nextConnections[0] : null;
                if (prevConnections.Count + nextConnections.Count > 2)
                {
                    prev = null;
                    next = null;
                }
                Vector2d w; // points right
                if (next != null && prev != null)
                {
                    Vector2d w1 = (next.pos - pos).RotateCW90().Normalized() * width / 2;
                    Vector2d w2 = (pos - prev.pos).RotateCW90().Normalized() * width / 2;
                    w1.Y *= Math.Cos(pos.Y);
                    w2.Y *= Math.Cos(pos.Y);
                    w = (w1 + w2).Normalized() * width / 2;
                }
                else if (next != null)
                {
                    w = (next.pos - pos).RotateCW90().Normalized() * width / 2;
                }
                else if (prev != null)
                {
                    w = (pos - prev.pos).RotateCW90().Normalized() * width / 2;
                }
                else
                {
                    if (nextConnections.Contains(target))
                    {
                        w = (target.pos - pos).RotateCW90().Normalized() * width / 2;
                    }
                    else
                    {
                        w = -(target.pos - pos).RotateCW90().Normalized() * width / 2;
                    }
                }
                // adjust width based on longLat
                w.Y *= Math.Cos(pos.Y);
                return w;
            }
        }
    }
}

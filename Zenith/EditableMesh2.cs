using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith
{
    class EditableMesh2
    {

        public static int LL_SEGMENTS = 3; // we'll split it up into the mininum number of longitude slices to keep things smoothly cycling

        List<List<IntPoint>> solution = new List<List<IntPoint>>();
        private List<Vector2> triangles;
        List<IntPoint> diagonals;
        List<List<VertexPositionColor>> sections;
        private static float REZ = 100000f;
        //private Clipper clipper;

        public EditableMesh2()
        {
            //clipper = new Clipper();
            RecalculateTriangles();
            RecalculateOutline();
            RecalculateSections();
        }

        private class VertexInfo
        {
            public IntPoint prev;
            public IntPoint v;
            public IntPoint next;

            public VertexInfo(IntPoint prev, IntPoint v, IntPoint next)
            {
                this.prev = prev;
                this.v = v;
                this.next = next;
            }

            // TODO: make sure this makes sense
            internal bool IsClockwise() // we promise not to call this when the points are coliniear
            {
                long area = (v.X - prev.X) * (next.Y - v.Y) + (v.Y - prev.Y) * -(next.X - v.X);
                return area > 0;
            }
        }

        private class Status
        {
            VertexInfo helper;
            // the two points of the edge, I think we're ignoring order for now
            VertexInfo edge1;
            VertexInfo edge2;
        }

        private void RecalculateTriangles()
        {
            triangles = new List<Vector2>();
            diagonals = new List<IntPoint>();
            var eventList = new List<VertexInfo>();
            foreach (var polygon in solution)
            {
                for (int i = 0; i < polygon.Count; i++) // we're going to assume that these points are cw order, and ccw if a hole?
                {
                    // let's assume no duplicate vertices
                    IntPoint prev = polygon[(i + polygon.Count - 1) % polygon.Count];
                    IntPoint curr = polygon[i];
                    IntPoint next = polygon[(i + 1) % polygon.Count];
                    eventList.Add(new VertexInfo(prev, curr, next));
                }
            }
            //eventList.Sort((x, y) => x.v.Y.CompareTo(y.v.Y));
            foreach (var v in eventList)
            {
                bool isJoin = false;
                bool isJoinOrSplit = false;
                if (v.prev.Y < v.v.Y && v.next.Y <= v.v.Y && !v.IsClockwise()) // a "join", the left corner in the degenerate case
                {
                    // find the first edge to the left of this point that intersects, then make the bottom point the diagonal
                    isJoin = true;
                    isJoinOrSplit = true;
                }
                if (v.prev.Y > v.v.Y && v.next.Y >= v.v.Y && !v.IsClockwise()) // a "split", the left corner in the degenerate case
                {
                    // find the first edge to the left of this point that intersects, then make the top point the diagonal
                    isJoin = false;
                    isJoinOrSplit = true;
                }
                //if (!v.IsClockwise()) throw new NotImplementedException();
                if (isJoinOrSplit)
                {
                    IntPoint bestDiagonal = new IntPoint(0, 0);
                    double bestXIntersect = double.MinValue;
                    foreach (var v2 in eventList) // we'll just look at v and next
                    {
                        if ((v2.v.Y >= v.v.Y && v2.next.Y <= v.v.Y) || (v2.v.Y <= v.v.Y && v2.next.Y >= v.v.Y)) // not going to care about determining left/right side
                        {
                            // TODO: don't use double
                            // TODO: handle horizontal case
                            if (v2.v.Y == v2.next.Y)
                            {
                                if (v2.v.X > v.v.X && v2.next.X > v.v.X) continue;
                                if (v2.v.X < v.v.X && v2.next.X < v.v.X)
                                {
                                    if (Math.Max(v2.v.X, v2.next.X) > bestXIntersect)
                                    {
                                        bestXIntersect = Math.Max(v2.v.X, v2.next.X);
                                        bestDiagonal = v2.v.X > v2.next.X ? v2.v : v2.next;
                                    }
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                            else
                            {
                                double t = (v.v.Y - v2.v.Y) / (v2.next.Y - v2.v.Y);
                                double xIntersect = v2.v.X * (1 - t) + t * v2.next.X;
                                if (xIntersect >= v.v.X) continue;
                                if (xIntersect > bestXIntersect)
                                {
                                    bestXIntersect = xIntersect;
                                    bestDiagonal = isJoin ? (v2.next.Y > v2.v.Y ? v2.next : v2.v) : (v2.next.Y < v2.v.Y ? v2.next : v2.v);
                                }
                            }
                        }
                    }
                    diagonals.Add(v.v);
                    diagonals.Add(bestDiagonal);
                }
            }
        }

        internal void AddPolygon(List<Vector2> adding)
        {
            Clipper clipper = new Clipper();
            List<IntPoint> asIntPoints = new List<IntPoint>();
            foreach (var v in adding)
            {
                asIntPoints.Add(new IntPoint(v.X * REZ, v.Y * REZ));
            }
            clipper.Clear();
            clipper.AddPaths(solution, PolyType.ptSubject, true);
            clipper.AddPath(asIntPoints, PolyType.ptClip, true);
            List<List<IntPoint>> newsolution = new List<List<IntPoint>>();
            bool success = clipper.Execute(ClipType.ctUnion, newsolution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            solution = newsolution;
            RecalculateTriangles();
            RecalculateOutline();
            RecalculateSections();
        }

        // temporary debug function - displays a fat outline
        private void RecalculateOutline()
        {
            triangles = new List<Vector2>();
            foreach (var polygon in solution)
            {
                for (int i = 0; i < polygon.Count; i++)
                {
                    Vector2 p1 = new Vector2(polygon[i].X / REZ, polygon[i].Y / REZ);
                    Vector2 p2 = new Vector2(polygon[(i + 1) % polygon.Count].X / REZ, polygon[(i + 1) % polygon.Count].Y / REZ);
                    Vector2 diff = p2 - p1;
                    diff = new Vector2(diff.Y, -diff.X); // rotate right 90 degrees, probably
                    diff.Normalize();
                    diff /= -2000; // hmm, not sure why my rotation assumption is wrong
                    triangles.Add(p2 - diff);
                    triangles.Add(p2 + diff);
                    triangles.Add(p1 + diff);
                    triangles.Add(p2 - diff);
                    triangles.Add(p1 + diff);
                    triangles.Add(p1 - diff);
                }
            }
            // now lets render those debug diagonals we've found
            for (int i = 0; i < diagonals.Count; i += 2)
            {
                // copy-pasted of course
                Vector2 p1 = new Vector2(diagonals[i].X / REZ, diagonals[i].Y / REZ);
                Vector2 p2 = new Vector2(diagonals[i + 1].X / REZ, diagonals[i + 1].Y / REZ);
                Vector2 diff = p2 - p1;
                diff = new Vector2(diff.Y, -diff.X); // rotate right 90 degrees, probably
                diff.Normalize();
                diff /= -2000; // hmm, not sure why my rotation assumption is wrong
                triangles.Add(p2 - diff);
                triangles.Add(p2 + diff);
                triangles.Add(p1 + diff);
                triangles.Add(p2 - diff);
                triangles.Add(p1 + diff);
                triangles.Add(p1 - diff);
            }
        }

        private void RecalculateSections()
        {
            sections = new List<List<VertexPositionColor>>();
            for (int i = 0; i < LL_SEGMENTS; i++) sections.Add(new List<VertexPositionColor>());
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector2 triCenter = (triangles[i] + triangles[i + 1] + triangles[i + 2]) / 3;
                int section = (int)((triCenter.X + Math.PI) / (2 * Math.PI) * LL_SEGMENTS);
                sections[section].Add(new VertexPositionColor(new Vector3(triangles[i].X, triangles[i].Y, 0), Color.Green));
                sections[section].Add(new VertexPositionColor(new Vector3(triangles[i + 1].X, triangles[i + 1].Y, 0), Color.Green));
                sections[section].Add(new VertexPositionColor(new Vector3(triangles[i + 2].X, triangles[i + 2].Y, 0), Color.Green));
            }
        }

        internal List<List<VertexPositionColor>> GetSections()
        {
            return sections;
        }
    }
}

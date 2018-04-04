using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith
{
    public class PointPointer
    {
        public Vector2 p;
        public PointPointer head;
        private PointPointer _next;
        public PointPointer next
        {
            get { return _next; }
            set
            {
                if (value == this) throw new NotImplementedException();
                _next = value;
            }
        }
        //public PointPointer next; // I guess we'll make this never be null
    }
    class EditableMesh
    {
        private PointPointer INFINITE = new PointPointer();
        private List<PointPointer> polygons = new List<PointPointer>();
        private List<Vector2> triangles;
        List<List<VertexPositionColor>> sections;

        public static int LL_SEGMENTS = 3; // we'll split it up into the mininum number of longitude slices to keep things smoothly cycling

        public EditableMesh()
        {
            INFINITE.p = new Vector2(10000, 10000);
            RecalculateOutline();
            RecalculateSections();
            // test, let's make it easy and use diagonal lines instead of vertical/horizontal
            //AddADiamond(new Vector2(0, 0), 0.2f);
            //AddADiamond(new Vector2(-0.05f, -0.05f), 0.2f);
            //AddADiamond(new Vector2(0.3f, -0.2f), 0.2f);
            //AddADiamond(new Vector2(0.1f, -0.2f), 0.2f);
        }

        // debug method
        private void AddADiamond(Vector2 offset, float size)
        {
            // size is 0.4 by 0.4
            List<Vector2> square1 = new List<Vector2>();
            square1.Add(new Vector2(size, 0) + offset); // right point first
            square1.Add(new Vector2(0f, -size) + offset);
            square1.Add(new Vector2(-size, 0) + offset);
            square1.Add(new Vector2(0, size) + offset);
            AddPolygon(square1);
        }

        internal List<PointPointer> MakeIntoPointers(List<Vector2> vs)
        {
            PointPointer[] pointers = new PointPointer[vs.Count];
            pointers[0] = new PointPointer();
            pointers[0].p = vs[0];
            pointers[0].head = pointers[0];
            for (int i = 1; i < vs.Count; i++)
            {
                var newpoint = new PointPointer();
                newpoint.p = vs[i];
                newpoint.head = pointers[0];
                pointers[i] = newpoint;
                pointers[i - 1].next = newpoint;
            }
            pointers[vs.Count - 1].next = pointers[0];
            return pointers.ToList();
        }

        internal void AddPolygon(List<Vector2> adding) // must be clockwise to add, probably
        {
            //polygons.Add(polygon);
            List<PointPointer> newpolygonpoints = new List<PointPointer>();
            List<PointPointer> newpolygon = MakeIntoPointers(adding);
            bool isInside = GetAllIntersections(INFINITE, newpolygon[0]).Count() % 2 == 1;
            HashSet<PointPointer> polygonsToRemove = new HashSet<PointPointer>();
            // TODO: calculate "isInside"
            List<Intersection>[] intersectionss = new List<Intersection>[adding.Count];
            for (int i = 0; i < adding.Count; i++)
            {
                // have to precalculate intersections since we modify the data as we go through
                intersectionss[i] = GetAllIntersections(newpolygon[i], newpolygon[(i + 1) % adding.Count]);
            }
            for (int i = 0; i < adding.Count; i++)
            {
                List<Intersection> intersections = intersectionss[i];
                // sort by closest to adding[i] (the start of our line)
                intersections.Sort((x, y) => (x.p - adding[i]).Length().CompareTo((y.p - adding[i]).Length()));
                if (!isInside)
                {
                    newpolygonpoints.Add(newpolygon[i]);
                }
                bool nextIsInside = isInside ? (intersections.Count() % 2 == 0) : (intersections.Count() % 2 == 1);
                if (!isInside && intersections.Count > 0)
                {
                    PointPointer newIntPoint = new PointPointer();
                    newIntPoint.p = intersections[0].p;
                    newIntPoint.next = intersections[0].d;
                    newpolygon[i].next = newIntPoint;
                    polygonsToRemove.Add(intersections[0].c.head);
                    intersections.RemoveAt(0);
                }
                while (intersections.Count >= 2)
                {
                    Intersection i1 = intersections[0];
                    Intersection i2 = intersections[1];
                    intersections.RemoveRange(0, 2);
                    PointPointer newIntPoint = new PointPointer();
                    newIntPoint.p = i1.p;
                    newpolygonpoints.Add(newIntPoint);
                    PointPointer newIntPoint2 = new PointPointer();
                    newIntPoint.next = newIntPoint2;
                    i1.c.next = newIntPoint;
                    newIntPoint2.p = i2.p;
                    newIntPoint2.next = i2.d;
                    polygonsToRemove.Add(i1.c.head);
                    polygonsToRemove.Add(i2.c.head);
                }
                if (intersections.Count == 1)
                {
                    newpolygonpoints.Add(newpolygon[i].next);
                    PointPointer newIntPoint = new PointPointer();
                    intersections[0].c.next = newIntPoint;
                    newIntPoint.p = intersections[0].p;
                    newIntPoint.next = newpolygon[(i + 1) % newpolygon.Count]; // BUG: was calling newpolygon[i].next, but it might've been modified!
                }
                isInside = nextIsInside;
            }
            if (newpolygonpoints.Count > 0)
            {
                foreach (var p in newpolygonpoints)
                {
                    SetHead(p, p); // just cheese it
                }

                foreach (var p in polygons) // check for polygons completely within our shape
                {
                    int count = 0;
                    for (int i = 0; i < newpolygon.Count; i++)
                    {
                        if (Intersect(newpolygon[i], newpolygon[(i + 1) % newpolygon.Count], p, INFINITE) != null)
                        {
                            count++;
                        }
                    }
                    if (count % 2 == 1)
                    {
                        polygonsToRemove.Add(p);
                    }
                }
                foreach (var p in polygonsToRemove) polygons.Remove(p);

                foreach (var p in newpolygonpoints)
                {
                    if (!polygons.Contains(p.head)) polygons.Add(p.head); // just cheese it
                }
            }
            RecalculateOutline();
            RecalculateSections();
        }

        private void SetHead(PointPointer anyPoint, PointPointer newHead)
        {
            PointPointer next = anyPoint;
            while (true)
            {
                next.head = newHead;
                next = next.next;
                if (next == anyPoint) return;
            }
        }

        private int DebugCount(PointPointer p)
        {
            PointPointer next = p;
            for (int i = 0; i < 1000; i++)
            {
                next = next.next;
                if (next == p) return i;
            }
            return 1000;
        }

        private List<Intersection> GetAllIntersections(PointPointer a, PointPointer b)
        {
            var intersections = new List<Intersection>();
            for (int j = 0; j < polygons.Count; j++)
            {
                PointPointer next = polygons[j];
                while (true)
                {
                    Intersection inter = Intersect(a, b, next, next.next);
                    if (inter != null) intersections.Add(inter);
                    next = next.next;
                    if (polygons[j] == next) break;
                }
            }
            return intersections;
        }

        // temporary debug function - only works accurately with convex shapes
        private void SimpleRecalculateTriangles()
        {
            triangles = new List<Vector2>();
            foreach (var head in polygons)
            {
                var next = head.next;
                while (true)
                {
                    triangles.Add(head.p);
                    triangles.Add(next.p);
                    triangles.Add(next.next.p);
                    next = next.next;
                    if (next == head) break;
                }
            }
        }

        // temporary debug function - displays a fat outline
        private void RecalculateOutline()
        {
            triangles = new List<Vector2>();
            foreach (var head in polygons)
            {
                var next = head;
                while (true)
                {
                    Vector2 p1 = next.p;
                    Vector2 p2 = next.next.p;
                    Vector2 diff = p2 - p1;
                    diff = new Vector2(diff.Y, -diff.X); // rotate right 90 degrees, probably
                    diff.Normalize();
                    diff /= -200; // hmm, not sure why my rotation assumption is wrong
                    triangles.Add(p2 - diff);
                    triangles.Add(p2 + diff);
                    triangles.Add(p1 + diff);
                    triangles.Add(p2 - diff);
                    triangles.Add(p1 + diff);
                    triangles.Add(p1 - diff);
                    next = next.next;
                    if (next == head) break;
                }
            }
        }

        private void RecalculateTriangles()
        {
            triangles = new List<Vector2>();
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

        private class Intersection // just holds metadata
        {
            public Vector2 p;
            public PointPointer c;
            public PointPointer d;

            public Intersection(Vector2 intersection, PointPointer c, PointPointer d)
            {
                this.p = intersection;
                this.c = c;
                this.d = d;
            }
        }

        private static Intersection Intersect(PointPointer a, PointPointer b, PointPointer c, PointPointer d)
        {
            // eh just copy paste from wikipedia
            //float n1 = a.p.X * b.p.Y - a.p.Y * b.p.X;
            //float n2 = c.p.X * d.p.Y - c.p.Y * d.p.X;
            //float denom = (a.p.X - b.p.X) * (c.p.Y - d.p.Y) - (a.p.Y - b.p.Y) * (c.p.X - d.p.X);
            //if (denom == 0) return null;
            //Vector2 intersection = (n1 * (c.p - d.p) - (a.p - b.p) * n2) / denom;
            //if (intersection.X >= a.p.X && intersection.X <= b.p.X || intersection.Y >= a.p.Y && intersection.Y <= b.p.Y)
            //{
            //    if (intersection.X >= c.p.X && intersection.X <= d.p.X || intersection.Y >= c.p.Y && intersection.Y <= d.p.Y)
            //    {
            //        return new Intersection(intersection, c, d);
            //    }
            //}

            // a.x+(b.x-a.x)*t=c.x+(d.x-c.x)*g
            // a.y+(b.y-a.y)*t=c.y+(d.y-c.y)*g
            // (a.x+(b.x-a.x)*t-c.x)/(d.x-c.x) = (a.y+(b.y-a.y)*t-c.y)/(d.y-c.y)
            // (a.x+(b.x-a.x)*t-c.x)*(d.y-c.y) = (d.x-c.x)*(a.y+(b.y-a.y)*t-c.y)
            // at+b=0
            double numA = (b.p.X - a.p.X) * (d.p.Y - c.p.Y) - (d.p.X - c.p.X) * (b.p.Y - a.p.Y);
            double numB = (a.p.X - c.p.X) * (d.p.Y - c.p.Y) - (d.p.X - c.p.X) * (a.p.Y - c.p.Y);
            double t = -numB / numA;
            double numAg = (d.p.X - c.p.X) * (b.p.Y - a.p.Y) - (b.p.X - a.p.X) * (d.p.Y - c.p.Y); // just swapped the points
            double numBg = (c.p.X - a.p.X) * (b.p.Y - a.p.Y) - (b.p.X - a.p.X) * (c.p.Y - a.p.Y);
            double g = -numBg / numAg;
            if (t >= 0 && t <= 1 && g >= 0 && g <= 1) return new Intersection(a.p + (b.p - a.p) * (float)t, c, d);
            return null;
        }
    }
}

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
        List<List<VertexPositionColor>> sections;
        private static float REZ = 100000f;
        //private Clipper clipper;

        public EditableMesh2()
        {
            //clipper = new Clipper();
            RecalculateOutline();
            RecalculateSections();
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
                    diff /= -200; // hmm, not sure why my rotation assumption is wrong
                    triangles.Add(p2 - diff);
                    triangles.Add(p2 + diff);
                    triangles.Add(p1 + diff);
                    triangles.Add(p2 - diff);
                    triangles.Add(p1 + diff);
                    triangles.Add(p1 - diff);
                }
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

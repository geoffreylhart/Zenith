using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace Zenith.ZMath
{
    // note: I believe I've decided to go against normal cube-mapping
    // instead of mapping a cube to a sphere as if it's been blown out, I evenly distribute the angles of my longitudes/latitudes like they are in other projections
    // this means pixels will have the same area around the equator, meridian, etc.
    public class CubeSector : ISector
    {
        public CubeSectorFace sectorFace;
        public int X { get => x; set => x = value; } // measured 0,1,2,3 based on ZCoords config
        public int Y { get => y; set => y = value; } // measured 0,1,2,3 based on ZCoords config
        public int Zoom { get => zoom; set => zoom = value; }

        public double SurfaceAreaPortion => throw new NotImplementedException();

        public LongLat TopLeftCorner => throw new NotImplementedException();

        public LongLat TopRightCorner => throw new NotImplementedException();

        public LongLat BottomLeftCorner => throw new NotImplementedException();

        public LongLat BottomRightCorner => throw new NotImplementedException();

        public double Longitude => throw new NotImplementedException();

        public double Latitude => throw new NotImplementedException();

        private int x;
        private int y;
        private int zoom; // each face is partitioned into 2^zoom vertical and horizontal sections

        public CubeSector(CubeSectorFace sectorFace, int x, int y, int zoom)
        {
            this.sectorFace = sectorFace;
            this.x = x;
            this.y = y;
            this.zoom = zoom;
        }

        public List<ISector> GetChildrenAtLevel(int z)
        {
            List<ISector> answer = new List<ISector>();
            if (z > zoom)
            {
                int diffPow = 1 << (z - zoom);
                for (int i = 0; i < diffPow; i++)
                {
                    for (int j = 0; j < diffPow; j++)
                    {
                        answer.Add(new CubeSector(sectorFace, x * diffPow + i, y * diffPow + j, z));
                    }
                }
            }
            return answer;
        }

        public List<ISector> GetAllParents()
        {
            List<ISector> answer = new List<ISector>();
            for (int i = 1; i <= zoom; i++)
            {
                answer.Add(new CubeSector(sectorFace, x >> i, y >> i, zoom - i));
            }
            return answer;
        }

        // TODO: all of these assume a zoom lower than current right now
        public int GetRelativeXOf(ISector s)
        {
            int diff = s.Zoom - Zoom;
            return s.X - X * (1 << diff);
        }

        public int GetRelativeYOf(ISector s)
        {
            int diff = s.Zoom - Zoom;
            return s.Y - Y * (1 << diff);
        }

        public bool ContainsLongLat(LongLat longLat)
        {
            Vector3d longLat3d = longLat.ToSphereVector();
            Vector3d up = sectorFace.GetFaceUpDirection();
            Vector3d right = sectorFace.GetFaceRightDirection();
            Vector3d normal = sectorFace.GetFaceNormal();
            double relX = (GetRel(normal, right, longLat3d) + 1) / 2 * (1 << Zoom);
            double relY = (GetRel(normal, up, longLat3d) + 1) / 2 * (1 << Zoom);
            if (relX > X + 1 || relX < X) return false;
            if (relY > Y + 1 || relY < Y) return false;
            // TODO: actual subsectors
            return true;
        }

        // return the portion x is betweeen from and to (negative values allowed)
        // portion is angular portion (as opposed to sin or w/e)
        public double GetRel(Vector3d from, Vector3d to, Vector3d x)
        {
            from = from.Normalized();
            to = to.Normalized();
            Plane plane = new Plane(new Vector3d(0, 0, 0), from.Cross(to)); // doesn't matter if the normal is negative or positive
            var projected = plane.Project(x).Normalized();
            return Math.Asin(projected.Cross(from).Dot(to.Cross(from))) / (Math.PI / 4);
        }

        public override string ToString()
        {
            return $"{this.sectorFace.GetFaceAcronym()},X={x},Y={y},Zoom={zoom}";
        }

        public LongLat[] GetIntersections(LongLat ll1, LongLat ll2)
        {
            throw new NotImplementedException();
        }

        public enum CubeSectorFace
        {
            FRONT, BACK, LEFT, RIGHT, TOP, BOTTOM
        }
    }
}

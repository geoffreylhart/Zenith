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

        public double ZoomPortion { get { return Math.Pow(0.5, zoom); } }

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
            double relX = (GetRel(normal, right, longLat3d) + 0.5) * (1 << Zoom);
            double relY = (GetRel(normal, up, longLat3d) + 0.5) * (1 << Zoom);
            if (relX > X + 1 || relX < X) return false;
            if (relY > Y + 1 || relY < Y) return false;
            // TODO: actual subsectors
            return true;
        }

        public bool ContainsRootCoord(Vector2d v)
        {
            if (v.X < x * ZoomPortion || v.X > (x + 1) * ZoomPortion) return false;
            if (v.Y < y * ZoomPortion || v.Y > (y + 1) * ZoomPortion) return false;
            return true;
        }

        // return the portion x is betweeen from and to (negative values allowed)
        // portion is angular portion (as opposed to sin or w/e)
        // we're changing this to require from and to to be right angles now...
        public double GetRel(Vector3d from, Vector3d to, Vector3d x)
        {
            from = from.Normalized();
            to = to.Normalized();
            x = x.Normalized();
            double xComp = x.Dot(from);
            double yComp = x.Dot(to);
            return Math.Atan2(yComp, xComp) / (Math.PI / 2);
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

        public override bool Equals(object obj)
        {
            CubeSector that = (CubeSector)obj;
            if (this.x != that.x) return false;
            if (this.y != that.y) return false;
            if (this.zoom != that.zoom) return false;
            if (this.sectorFace != that.sectorFace) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return ((x * 31 + y) * 31 + zoom) * 31 + (int)sectorFace;
        }

        public List<ISector> GetSectorsInRange(double minX, double maxX, double minY, double maxY, int zoom)
        {
            if (minX > 1) return new List<ISector>();
            if (maxX < 0) return new List<ISector>();
            if (minY > 1) return new List<ISector>();
            if (maxY < 0) return new List<ISector>();
            if (zoom < this.zoom) throw new NotImplementedException();
            int powDiff = 1 << (zoom - this.zoom);
            int minXR = (int)Math.Floor(Math.Max(Math.Min(minX, 1), 0) * powDiff + this.x * powDiff);
            int maxXR = (int)Math.Ceiling(Math.Max(Math.Min(maxX, 1), 0) * powDiff + this.x * powDiff);
            int minYR = (int)Math.Floor(Math.Max(Math.Min(minY, 1), 0) * powDiff + this.y * powDiff);
            int maxYR = (int)Math.Ceiling(Math.Max(Math.Min(maxY, 1), 0) * powDiff + this.y * powDiff);
            List<ISector> containedSectors = new List<ISector>();
            for (int i = minXR; i < maxXR; i++)
            {
                for (int j = minYR; j < maxYR; j++)
                {
                    containedSectors.Add(new CubeSector(this.sectorFace, i, j, zoom));
                }
            }
            return containedSectors;
        }

        public Vector2d ProjectToLocalCoordinates(Vector3d v)
        {
            Vector3d up = sectorFace.GetFaceUpDirection();
            Vector3d right = sectorFace.GetFaceRightDirection();
            Vector3d normal = sectorFace.GetFaceNormal();
            double relX = (GetRel(normal, right, v) + 0.5) * (1 << Zoom);
            double relY = (GetRel(normal, up, v) + 0.5) * (1 << Zoom);
            return new Vector2d(relX - X, relY - Y);
        }

        public Vector3d ProjectToSphereCoordinates(Vector2d v)
        {
            Vector3d up = sectorFace.GetFaceUpDirection();
            Vector3d right = sectorFace.GetFaceRightDirection();
            Vector3d normal = sectorFace.GetFaceNormal();
            // lets just do the opposite of ProjectToLocalCoordinates
            double relX = ((v.X + X) / (1 << Zoom) - 0.5); // remember, this is the portion of the angle we're from normal to right
            double relY = ((v.Y + Y) / (1 << Zoom) - 0.5); // remember, this is the portion of the angle we're from normal to up
            Vector3d newup = RotatePortion(up, -normal, relY);
            Vector3d newright = RotatePortion(right, -normal, relX);
            return newright.Cross(newup).Normalized(); // similar to the logic for doing a plane intersection
        }

        // we're changing this to require from and to to be right angles now...
        private Vector3d RotatePortion(Vector3d from, Vector3d to, double x)
        {
            from = from.Normalized();
            to = to.Normalized();
            return from * Math.Cos(x * Math.PI / 2) + to * Math.Sin(x * Math.PI / 2);
        }

        public ISector GetSectorAt(double x, double y, int zoom)
        {
            if (x > 1) return null;
            if (x < 0) return null;
            if (y > 1) return null;
            if (y < 0) return null;
            if (zoom < this.zoom) throw new NotImplementedException();
            int powDiff = 1 << (zoom - this.zoom);
            int xr = (int)Math.Floor(Math.Max(Math.Min(x, 1), 0) * powDiff + this.x * powDiff);
            int yr = (int)Math.Floor(Math.Max(Math.Min(y, 1), 0) * powDiff + this.y * powDiff);
            return new CubeSector(this.sectorFace, xr, yr, zoom);
        }

        public ISector GetRoot()
        {
            return new CubeSector(sectorFace, 0, 0, 0);
        }
    }
}

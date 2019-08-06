using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Zenith.MathHelpers;

namespace Zenith.ZMath
{
    public class MercatorSector : ISector
    {
        public int X { get => x; set => x = value; } // measured 0,1,2,3 based on ZCoords config
        public int Y { get => y; set => y = value; } // measured 0,1,2,3 based on ZCoords config
        public int Zoom { get => zoom; set => zoom = value; }
        private int x; // measured 0,1,2,3 from -pi to pi (opposite left to opposite right of prime meridian)
        private int y; // measured 0,1,2,3 from pi/2 (north pole) to -pi/2 (south pole)
        private int zoom; // each face is partitioned into 2^zoom vertical and horizontal sections

        public MercatorSector(int x, int y, int zoom)
        {
            this.x = x;
            this.y = y;
            this.zoom = zoom;
        }

        public double ZoomPortion { get { return Math.Pow(0.5, zoom); } }
        public double Longitude { get { return GetLongLat(0.5, 0.5).X; } }
        public double LeftLongitude { get { return GetLongLat(0, 0).X; } }
        public double RightLongitude { get { return GetLongLat(1, 1).X; } }
        public double Latitude { get { return GetLongLat(0.5, 0.5).Y; } }
        public double TopLatitude { get { return GetLongLat(0, 0).Y; } }
        public double BottomLatitude { get { return GetLongLat(1, 1).Y; } }

        // from local coordinates
        private LongLat GetLongLat(double x, double y)
        {
            return ((SphereVector)ProjectToSphereCoordinates(new Vector2d(x, y))).ToLongLat();
        }

        public double SurfaceAreaPortion // where 1 is the whole sphere
        {
            get
            {
                // A=2pi*r^2*(1-cos0) wiki
                double capDiff = 2 * Math.PI * (Math.Sin(TopLatitude) - Math.Sin(BottomLatitude));
                double sa = capDiff * ZoomPortion;
                return sa / (4 * Math.PI);
            }
        }

        public override string ToString()
        {
            return $"X={x},Y={y},Zoom={zoom}";
        }

        public bool ContainsSector(MercatorSector sector)
        {
            if (sector.zoom < zoom) return false;
            if ((sector.x >> (sector.zoom - zoom)) != x) return false;
            if ((sector.y >> (sector.zoom - zoom)) != y) return false;
            return true;
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
                        answer.Add(new MercatorSector(x * diffPow + i, y * diffPow + j, z));
                    }
                }
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

        public List<ISector> GetAllParents()
        {
            List<ISector> answer = new List<ISector>();
            for (int i = 1; i <= zoom; i++)
            {
                answer.Add(new MercatorSector(x >> i, y >> i, zoom - i));
            }
            return answer;
        }

        public bool ContainsLongLat(LongLat longLat)
        {
            if (longLat.X < LeftLongitude || longLat.X > RightLongitude) return false;
            if (longLat.Y < BottomLatitude || longLat.Y > TopLatitude) return false;
            return true;
        }

        public bool ContainsRootCoord(Vector2d v)
        {
            if (v.X < x * ZoomPortion || v.X > (x + 1) * ZoomPortion) return false;
            if (v.Y < y * ZoomPortion || v.Y > (y + 1) * ZoomPortion) return false;
            return true;
        }

        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (0.5 - y) * 2 * Math.PI)) - Math.PI / 2;
        }

        // takes -pi/2 to pi/2, I assume, goes from infinity to -infinity??
        // goes from 1 to 0 in the cutoff range of 85.051129 degrees
        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(-lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }

        public override bool Equals(object obj)
        {
            MercatorSector that = (MercatorSector)obj;
            if (this.x != that.x) return false;
            if (this.y != that.y) return false;
            if (this.zoom != that.zoom) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return (x * 31 + y) * 31 + zoom;
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
                    containedSectors.Add(new MercatorSector(i, j, zoom));
                }
            }
            return containedSectors;
        }

        public Vector2d ProjectToLocalCoordinates(Vector3d v)
        {
            LongLat longLat = new SphereVector(v).ToLongLat();
            double localX = (longLat.X + Math.PI) / (ZoomPortion * 2 * Math.PI) - x;
            double localY = ToY(longLat.Y) / ZoomPortion - y;
            return new Vector2d(localX, localY);
        }

        public Vector3d ProjectToSphereCoordinates(Vector2d v)
        {
            double longitude = (x + v.X) * (ZoomPortion * 2 * Math.PI) - Math.PI;
            double latitude = ToLat((y + v.Y) * (ZoomPortion));
            return new LongLat(longitude, latitude).ToSphereVector();
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
            return new MercatorSector(xr, yr, zoom);
        }

        public ISector GetRoot()
        {
            return new MercatorSector(0, 0, 0);
        }
    }
}

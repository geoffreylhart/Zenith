using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.ZMath
{
    public class Sector
    {
        public int x; // measured 0,1,2,3 from -pi to pi (opposite left to opposite right of prime meridian)

        internal double MinDistanceFrom(LongLat longLat)
        {
            if (longLat.X >= LeftLongitude && longLat.X <= RightLongitude)
            {
                if (longLat.Y >= BottomLatitude && longLat.Y <= TopLatitude)
                {
                    return 0;
                }
                else
                {
                    return Math.Min(Math.Abs(TopLatitude - longLat.Y), Math.Abs(BottomLatitude - longLat.Y));
                }
            }
            else
            {
                if (longLat.Y >= BottomLatitude && longLat.Y <= TopLatitude)
                {
                    return Math.Min(Math.Abs(LeftLongitude - longLat.X), Math.Abs(RightLongitude - longLat.X));
                }
                else
                {
                    SphereVector asSphereVec = longLat.ToSphereVector();
                    double dis = TopLeftCorner.ToSphereVector().Distance(asSphereVec);
                    dis = Math.Min(dis, TopRightCorner.ToSphereVector().Distance(asSphereVec));
                    dis = Math.Min(dis, BottomLeftCorner.ToSphereVector().Distance(asSphereVec));
                    dis = Math.Min(dis, BottomRightCorner.ToSphereVector().Distance(asSphereVec));
                    return dis;
                }
            }
        }

        public int y; // measured 0,1,2,3 from -pi/2 (south pole) to pi/2 (north pole)
        public int zoom; // the globe is partitioned into 2^zoom vertical and horizontal sections

        public Sector(int x, int y, int zoom)
        {
            this.x = x;
            this.y = y;
            this.zoom = zoom;
        }

        public double ZoomPortion { get { return Math.Pow(0.5, zoom); } }
        public double Longitude { get { return (x + 0.5) * (ZoomPortion * 2 * Math.PI) - Math.PI; } }
        public double LeftLongitude { get { return x * (ZoomPortion * 2 * Math.PI) - Math.PI; } }
        public double RightLongitude { get { return (x + 1) * (ZoomPortion * 2 * Math.PI) - Math.PI; } }
        public double Latitude { get { return ToLat((y + 0.5) * (ZoomPortion)); } }
        public double TopLatitude { get { return ToLat((y + 1) * (ZoomPortion)); } }
        public double BottomLatitude { get { return ToLat(y * (ZoomPortion)); } }
        public LongLat TopLeftCorner { get { return new LongLat(LeftLongitude, TopLatitude); } }
        public LongLat TopRightCorner { get { return new LongLat(RightLongitude, TopLatitude); } }
        public LongLat BottomLeftCorner { get { return new LongLat(LeftLongitude, BottomLatitude); } }
        public LongLat BottomRightCorner { get { return new LongLat(RightLongitude, BottomLatitude); } }

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

        public bool ContainsSector(Sector sector)
        {
            if (sector.zoom < zoom) return false;
            if ((sector.x >> (sector.zoom - zoom)) != x) return false;
            if ((sector.y >> (sector.zoom - zoom)) != y) return false;
            return true;
        }

        internal List<Sector> GetChildrenAtLevel(int z)
        {
            List<Sector> answer = new List<Sector>();
            if (z > zoom)
            {
                int diffPow = 1 << (z - zoom);
                for (int i = 0; i < diffPow; i++)
                {
                    for (int j = 0; j < diffPow; j++)
                    {
                        answer.Add(new Sector(x * diffPow + i, y * diffPow + j, z));
                    }
                }
            }
            return answer;
        }

        // TODO: all of these assume a zoom lower than current right now
        internal int GetRelativeXOf(Sector s)
        {
            int diff = s.zoom - zoom;
            return s.x - x * (1 << diff);
        }

        internal int GetRelativeYOf(Sector s)
        {
            int diff = s.zoom - zoom;
            return s.y - y * (1 << diff);
        }

        internal IEnumerable<Sector> GetAllParents()
        {
            List<Sector> answer = new List<Sector>();
            for (int i = 1; i <= zoom; i++)
            {
                answer.Add(new Sector(x >> i, y >> i, zoom - i));
            }
            return answer;
        }

        internal bool ContainsLongLat(LongLat longLat)
        {
            if (longLat.X < LeftLongitude || longLat.X > RightLongitude) return false;
            if (longLat.Y < BottomLatitude || longLat.Y > TopLatitude) return false;
            return true;
        }

        // do we treat these as straight lines or arc lines?
        // I guess lets do straight lines
        // let's return them in order of intersection
        internal LongLat[] GetIntersections(LongLat start, LongLat end)
        {
            List<LongLat> answer = new List<LongLat>();
            answer.AddRange(GetIntersections(start, end, TopLeftCorner, TopRightCorner));
            answer.AddRange(GetIntersections(start, end, TopRightCorner, BottomRightCorner));
            answer.AddRange(GetIntersections(start, end, BottomRightCorner, BottomLeftCorner));
            answer.AddRange(GetIntersections(start, end, BottomLeftCorner, TopLeftCorner));
            answer.Sort((x, y) => (Math.Pow(x.X - start.X, 2) * Math.Pow(x.Y - start.Y, 2)).CompareTo(Math.Pow(y.X - start.X, 2) * Math.Pow(y.Y - start.Y, 2)));
            return answer.ToArray();
        }

        private LongLat[] GetIntersections(LongLat A, LongLat B, LongLat C, LongLat D)
        {
            LongLat CmP = new LongLat(C.X - A.X, C.Y - A.Y);
            LongLat r = new LongLat(B.X - A.X, B.Y - A.Y);
            LongLat s = new LongLat(D.X - C.X, D.Y - C.Y);

            double CmPxr = CmP.X * r.Y - CmP.Y * r.X;
            double CmPxs = CmP.X * s.Y - CmP.Y * s.X;
            double rxs = r.X * s.Y - r.Y * s.X;

            double rxsr = 1f / rxs;
            double t = CmPxs * rxsr;
            double u = CmPxr * rxsr;

            if ((t >= 0) && (t <= 1) && (u >= 0) && (u <= 1))
            {
                return new[] { new LongLat(A.X * (1 - t) + B.X * t, A.Y * (1 - t) + B.Y * t) };
            }
            else
            {
                return new LongLat[0];
            }
        }


        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }

        // takes -pi/2 to pi/2, I assume, goes from -infinity to infinity??
        // goes from 0 to 1 in the cutoff range of 85.051129 degrees
        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }

        public override bool Equals(object obj)
        {
            Sector that = (Sector)obj;
            if (this.x != that.x) return false;
            if (this.y != that.y) return false;
            if (this.zoom != that.zoom) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return (x * 31 + y) * 31 + zoom;
        }
    }
}

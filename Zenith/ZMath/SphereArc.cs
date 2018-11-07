using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;

namespace Zenith.ZMath
{
    // represents any circle that lies on the unit sphere
    class SphereArc
    {
        public Circle3 intersection;
        public Vector3d start;
        public Vector3d stop;
        public bool shortPath; // there are two possible arcs otherwise

        public SphereArc(Circle3 intersection, Vector3d start, Vector3d stop, bool shortPath)
        {
            this.intersection = intersection;
            this.start = start;
            this.stop = stop;
            this.shortPath = shortPath;
        }

        // TODO: probably make special classes for sphere coordinates and lat/long, etc.
        private Vector3d ToLatLong(Vector3d v)
        {
            return new Vector3d(Math.Atan2(v.X, -v.Y), Math.Asin(v.Z), 0);
        }

        // not evenly spaced
        private List<Vector3d> MakeLongLats(int x) // x = sections
        {
            if (!shortPath) throw new NotImplementedException();
            List<Vector3d> longLats = new List<Vector3d>();
            for (int i = 0; i <= x; i++)
            {
                longLats.Add(ToLatLong((((start - intersection.center) * i + (stop - intersection.center) * (x - i)) / x).Normalized() * intersection.radius + intersection.center));
            }
            return longLats;
        }

        internal double MinLong()
        {
            if (IntersectsSeam()) return -Math.PI;
            // TODO: fix issue where tangentPoint effectively has infinite z
            Vector3d tangentPoint = intersection.GetPlane().GetIntersection(new Vector3d(0, 0, 0), new Vector3d(0, 0, 1));
            Vector3d[] tangents = intersection.GetTangents(tangentPoint);
            double minLong = Math.Min(ToLatLong(start).X, ToLatLong(stop).X);
            foreach (var tangent in tangents)
            {
                if (InArc(tangent))
                {
                    minLong = Math.Min(minLong, ToLatLong(tangent).X);
                }
            }
            return minLong;
        }

        // assumes v lies on the circle containing this arc
        // v is a spherevector
        private bool InArc(Vector3d v)
        {
            if (v.Cross(start).Dot(v.Cross(stop)) < 0) return true;
            return false;
        }

        internal double MaxLong()
        {
            if (IntersectsSeam()) return Math.PI;
            Vector3d tangentPoint = intersection.GetPlane().GetIntersection(new Vector3d(0, 0, 0), new Vector3d(0, 0, 1));
            Vector3d[] tangents = intersection.GetTangents(tangentPoint);
            double maxLong = Math.Max(ToLatLong(start).X, ToLatLong(stop).X);
            foreach (var tangent in tangents)
            {
                if (InArc(tangent))
                {
                    maxLong = Math.Max(maxLong, ToLatLong(tangent).X);
                }
            }
            return maxLong;
        }

        private bool IntersectsSeam()
        {
            Plane seamPlane = new Plane(new Vector3d(0, 0, 0), new Vector3d(1, 0, 0));
            Vector3d[] intersections = intersection.GetIntersection(seamPlane);
            foreach(var v in intersections)
            {
                if (v.Y > 0 && this.InArc(v)) return true;
            }
            return false;
        }

        internal double MinLat()
        {
            return MakeLongLats(10).Min(x => x.Y);
        }

        internal double MaxLat()
        {
            return MakeLongLats(10).Max(x => x.Y);
        }
    }
}

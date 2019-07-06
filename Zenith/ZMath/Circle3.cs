using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;

namespace Zenith.ZMath
{
    public class Circle3
    {
        public Vector3d center;
        public Vector3d normal;
        public double radius;

        public Circle3(Vector3d center, Vector3d normal, double radius)
        {
            this.center = center;
            this.normal = normal.Normalized();
            this.radius = radius;
        }

        // treats v as if it were on the same plane and returns the position of the circle tangents in 3d space
        internal Vector3d[] GetTangents(Vector3d v)
        {
            v = GetPlane().Project(v);
            double distance = (v - center).Length();
            if (distance < radius) return new Vector3d[0]; // no tangents, I guess
            double angle = Math.Acos(radius / distance);
            Vector3d towardsVUnit = (v - center).Normalized();
            Vector3d towardsV = towardsVUnit * Math.Cos(angle) * radius + center;
            Vector3d perpendicular = Vector3d.Cross(normal, towardsVUnit) * Math.Sin(angle) * radius;
            return new[] { towardsV + perpendicular, towardsV - perpendicular };
        }

        public Plane GetPlane()
        {
            return new Plane(center, normal);
        }

        // TODO: probably make special classes for sphere coordinates and lat/long, etc.
        private Vector3d ToLatLong(Vector3d v)
        {
            return new Vector3d(Math.Atan2(v.X, -v.Y), Math.Asin(v.Z), 0);
        }

        // not evenly spaced
        private List<Vector3d> MakeVector3ds(int x) // x = sections
        {
            List<Vector3d> longLats = new List<Vector3d>();
            Random rand = new Random();
            Vector3d randV = new Vector3d(0, 0, 1);
            Vector3d randP = Vector3d.Cross(randV, normal).Normalized();
            Vector3d perp3 = Vector3d.Cross(randP, normal).Normalized();
            for (int i = 0; i < x; i++)
            {
                double angle = 2 * Math.PI * i / x;
                Vector3d pos = (Math.Sin(angle) * randP + Math.Cos(angle) * perp3) * radius + center;// random point on the circle
                longLats.Add(pos);
            }
            return longLats;
        }

        internal double MinLong()
        {
            if (IntersectsSeam()) return -Math.PI;
            Vector3d[] tangents = GetTangentsFromAxis();
            double minLong = 5;
            foreach (var tangent in tangents)
            {
                minLong = Math.Min(minLong, ToLatLong(tangent).X);
            }
            return minLong;
        }

        public Vector3d[] GetTangentsFromAxis()
        {
            Vector3d tangentPoint;
            if (Math.Abs(normal.Dot(new Vector3d(0, 0, 1))) == 0)
            {
                tangentPoint = new Vector3d(0, 0, 1000); // just make it far away for now
            }
            else
            {
                tangentPoint = GetPlane().GetIntersection(new Vector3d(0, 0, 0), new Vector3d(0, 0, 1));
            }
            return GetTangents(tangentPoint);
        }

        private bool IntersectsSeam()
        {
            Plane seamPlane = new Plane(new Vector3d(0, 0, 0), new Vector3d(1, 0, 0));
            Vector3d[] intersections = GetIntersection(seamPlane);
            foreach (var v in intersections)
            {
                if (v.Y > 0) return true;
            }
            return false;
        }

        internal double MaxLong()
        {
            if (IntersectsSeam()) return Math.PI;
            Vector3d[] tangents = GetTangentsFromAxis();
            double maxLong = -5;
            foreach (var tangent in tangents)
            {
                maxLong = Math.Max(maxLong, ToLatLong(tangent).X);
            }
            return maxLong;
        }

        // TODO: maybe don't rely on this
        internal double Min(Func<Vector3d, double> f)
        {
            return MakeVector3ds(10).Min(f);
        }

        internal double Max(Func<Vector3d, double> f)
        {
            return MakeVector3ds(10).Max(f);
        }

        internal Vector3d[] GetIntersection(Plane plane)
        {
            Vector3d perp = normal.Cross(plane.normal).Normalized();
            Vector3d notPerp = normal.Cross(perp).Normalized(); // should point towards the line of intersection between the planes
            Vector3d intersect = plane.GetIntersection(center, center + notPerp);
            double diffLenSquared = radius * radius - (intersect - center).LengthSquared();
            if (diffLenSquared < 0) return new Vector3d[0];
            double perpLen = Math.Sqrt(diffLenSquared);
            return new Vector3d[] { intersect + perp * perpLen, intersect - perp * perpLen };
        }
    }
}

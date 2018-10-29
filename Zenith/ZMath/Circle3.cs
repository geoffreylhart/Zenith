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
            if (distance < radius) throw new NotImplementedException();
            double angle = Math.Acos(radius / distance);
            Vector3d towardsVUnit = (v - center).Normalized();
            Vector3d towardsV = towardsVUnit * Math.Cos(angle) * radius + center;
            Vector3d perpendicular = Vector3d.Cross(normal, towardsVUnit) * Math.Sin(angle) * radius;
            return new[] { towardsV + perpendicular, towardsV - perpendicular };
        }

        private Plane GetPlane()
        {
            return new Plane(center, normal);
        }

        // TODO: probably make special classes for sphere coordinates and lat/long, etc.
        private Vector3d ToLatLong(Vector3d v)
        {
            return new Vector3d(Math.Atan2(v.X, -v.Y), Math.Asin(v.Z), 0);
        }

        // not evenly spaced
        private List<Vector3d> MakeLongLats(int x) // x = sections
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
                longLats.Add(ToLatLong(pos));
            }
            return longLats;
        }

        internal double MinLong()
        {
            return MakeLongLats(100).Min(x => x.X);
        }

        internal double MaxLong()
        {
            return MakeLongLats(100).Max(x => x.X);
        }

        internal double MinLat()
        {
            return MakeLongLats(100).Min(x => x.Y);
        }

        internal double MaxLat()
        {
            return MakeLongLats(100).Max(x => x.Y);
        }
    }
}

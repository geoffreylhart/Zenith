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
            Vector3d perpendicular = Vector3d.Cross(normal, towardsVUnit) * Math.Sin(angle);
            return new[] { towardsV + perpendicular, towardsV - perpendicular };
        }

        private Plane GetPlane()
        {
            return new Plane(center, normal);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.MathHelpers;

namespace Zenith.ZMath
{
    public class Plane
    {
        Vector3d center;
        Vector3d normal;

        public Plane(Vector3d center, Vector3d normal)
        {
            this.center = center;
            this.normal = normal.Normalized();
        }

        public Circle3 GetUnitSphereIntersection()
        {
            double distance = GetDistanceFromPoint(new Vector3d(0, 0, 0));
            double radius = Math.Sqrt(1-distance*distance);
            if (double.IsNaN(radius)) return null;
            Vector3d center = -normal*distance;
            return new Circle3(center, normal, radius);
        }

        internal Vector3d Project(Vector3d v)
        {
            return v - GetDistanceFromPoint(v) * normal;
        }

        // will return negative if "below" the plane
        public double GetDistanceFromPoint(Vector3d v)
        {
            return Vector3d.Dot(v - center, normal);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Zenith.MathHelpers;

namespace Zenith.ZMath
{
    // a 3d vector with length 1, meant to represent a point on a unit sphere
    public class SphereVector : Vector3d
    {
        public SphereVector(Vector3d v) : base(v.X, v.Y, v.Z)
        {
        }

        public SphereVector(Vector3 v) : base(v)
        {
        }

        public SphereVector(double x, double y, double z) : base(x, y, z)
        {
        }

        public SphereVector WalkNorth(double radians)
        {
            return WalkTowardsPortion(new SphereVector(0, 0, 1), radians);
        }

        public SphereVector WalkTowards(SphereVector v, double radians)
        {
            Vector3d D_tick = ((this.Cross(v).Cross(this)).Normalized()); // 90 degrees rotated towards v
            return new SphereVector(Math.Cos(radians) * this + Math.Sin(radians) * D_tick);
        }

        public SphereVector WalkTowardsPortion(SphereVector v, double portion)
        {
            return WalkTowards(v, this.Distance(v) * portion);
        }

        public LongLat ToLongLat()
        {
            return new LongLat(Math.Atan2(X, -Y), Math.Asin(Z));
        }

        public double Distance(SphereVector v)
        {
            return Math.Asin(this.Cross(v).Length());
        }
    }
}

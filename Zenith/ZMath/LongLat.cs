using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.ZMath
{
    public class LongLat : Vector2d
    {
        public LongLat(double x, double y) : base(x, y)
        {
        }

        internal SphereVector ToSphereVector()
        {
            double dz = Math.Sin(Y);
            double dxy = Math.Cos(Y); // the radius of the horizontal ring section, always positive
            double dx = Math.Sin(X) * dxy;
            double dy = -Math.Cos(X) * dxy;
            return new SphereVector(dx, dy, dz);
        }
    }
}

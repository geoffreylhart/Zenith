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
            return MakeLongLats(10).Min(x => x.X);
        }

        internal double MaxLong()
        {
            return MakeLongLats(10).Max(x => x.X);
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

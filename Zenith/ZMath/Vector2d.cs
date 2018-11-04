using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.ZMath
{
    public class Vector2d
    {
        public double X, Y;
        public Vector2d(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
        public static implicit operator Vector2(Vector2d v)
        {
            return new Vector2((float)v.X, (float)v.Y);
        }
    }
}

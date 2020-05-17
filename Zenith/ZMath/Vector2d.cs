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

        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public Vector2d(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
        public static implicit operator Vector2(Vector2d v)
        {
            return new Vector2((float)v.X, (float)v.Y);
        }

        public static Vector2d operator +(Vector2d value1, Vector2d value2)
        {
            return new Vector2d(value1.X + value2.X, value1.Y + value2.Y);
        }

        public static Vector2d operator -(Vector2d value)
        {
            return new Vector2d(-value.X, -value.Y);
        }

        public static Vector2d operator -(Vector2d value1, Vector2d value2)
        {
            return new Vector2d(value1.X - value2.X, value1.Y - value2.Y);
        }
        public static Vector2d operator *(Vector2d value, double scaleFactor)
        {

            return new Vector2d(value.X * scaleFactor, value.Y * scaleFactor);
        }

        public static Vector2d operator *(double scaleFactor, Vector2d value)
        {
            return new Vector2d(value.X * scaleFactor, value.Y * scaleFactor);
        }

        public static Vector2d operator /(Vector2d value, double divider)
        {
            return new Vector2d(value.X / divider, value.Y / divider);
        }

        public Vector2d Normalized()
        {
            return Normalize(this);
        }

        internal static Vector2d Normalize(Vector2d v)
        {
            double factor = Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
            return v / factor;
        }

        internal Vector2d RotateCW90()
        {
            return new Vector2d(this.Y, -this.X);
        }

        internal Vector2d RotateCCW90()
        {
            return new Vector2d(-this.Y, this.X);
        }

        public override int GetHashCode()
        {
            return this.X.GetHashCode() * 31 + this.Y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Vector2d that = (Vector2d)obj;
            return this.X == that.X && this.Y == that.Y;
        }
    }
}

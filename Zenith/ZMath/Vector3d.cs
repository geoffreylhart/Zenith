using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Zenith.MathHelpers
{
    public class Vector3d
    {
        public Vector3d(Vector3 v)
        {
            this.X = v.X;
            this.Y = v.Y;
            this.Z = v.Z;
        }

        // copied from monogame source but for doubles
        // oops, had to make it immutable since it's not a struct anymore
        public double X, Y, Z;

        public Vector3d(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X);
            sb.Append(" Y:");
            sb.Append(this.Y);
            sb.Append(" Z:");
            sb.Append(this.Z);
            sb.Append("}");
            return sb.ToString();
        }

        internal static Vector3d Transform(Vector3d position, Matrixd matrix)
        {
            var x = (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41;
            var y = (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42;
            var z = (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43;
            return new Vector3d(x, y, z);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)X, (float)Y, (float)Z);
        }

        public static double Dot(Vector3d value1, Vector3d value2)
        {
            return value1.X * value2.X + value1.Y * value2.Y + value1.Z * value2.Z;
        }

        public static Vector3d operator +(Vector3d value1, Vector3d value2)
        {
            return new Vector3d(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z);
        }

        public static Vector3d operator -(Vector3d value)
        {
            return new Vector3d(-value.X, -value.Y, -value.Z);
        }

        public static Vector3d operator -(Vector3d value1, Vector3d value2)
        {
            return new Vector3d(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z);
        }
        public static Vector3d operator *(Vector3d value, double scaleFactor)
        {

            return new Vector3d(value.X * scaleFactor, value.Y * scaleFactor, value.Z * scaleFactor);
        }

        public static Vector3d operator *(double scaleFactor, Vector3d value)
        {
            return new Vector3d(value.X * scaleFactor, value.Y * scaleFactor, value.Z * scaleFactor);
        }

        public static Vector3d operator /(Vector3d value, double divider)
        {
            return new Vector3d(value.X / divider, value.Y / divider, value.Z / divider);
        }

        public double Length()
        {
            return Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        }

        public double LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z);
        }

        public Vector3d Normalized()
        {
            return Normalize(this);
        }

        internal static Vector3d Normalize(Vector3d v)
        {
            double factor = Math.Sqrt((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z));
            return v / factor;
        }

        internal static Vector3d Cross(Vector3d vector1, Vector3d vector2)
        {
            var x = vector1.Y * vector2.Z - vector2.Y * vector1.Z;
            var y = -(vector1.X * vector2.Z - vector2.X * vector1.Z);
            var z = vector1.X * vector2.Y - vector2.X * vector1.Y;
            return new Vector3d(x, y, z);
        }

        public Vector3d Cross(Vector3d v)
        {
            return Cross(this, v);
        }
    }
}

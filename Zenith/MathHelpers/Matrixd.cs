using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.MathHelpers
{
    public class Matrixd
    {
        public double M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44;

        public Matrixd()
        {
        }

        public Matrixd(double m11, double m12, double m13, double m14, double m21, double m22, double m23, double m24, double m31,
                      double m32, double m33, double m34, double m41, double m42, double m43, double m44)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = m14;
            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = m24;
            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = m34;
            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;
            this.M44 = m44;
        }

        public Matrixd(Matrix m)
        {
            this.M11 = m.M11;
            this.M12 = m.M12;
            this.M13 = m.M13;
            this.M14 = m.M14;
            this.M21 = m.M21;
            this.M22 = m.M22;
            this.M23 = m.M23;
            this.M24 = m.M24;
            this.M31 = m.M31;
            this.M32 = m.M32;
            this.M33 = m.M33;
            this.M34 = m.M34;
            this.M41 = m.M41;
            this.M42 = m.M42;
            this.M43 = m.M43;
            this.M44 = m.M44;
        }

        internal static Vector3d Unproject(Viewport viewport, Vector3d source, Matrixd projection, Matrixd view, Matrixd world)
        {
            //return new Vector3d(viewport.Unproject(source.ToVector3(), projection.toMatrix(), view.toMatrix(), world.toMatrix()));
            Matrixd matrix = Matrixd.Invert(Matrixd.Multiply(Matrixd.Multiply(world, view), projection));
            Vector3d source2 = new Vector3d(0, 0, 0);
            source2.X = (((source.X - viewport.X) / (viewport.Width)) * 2f) - 1f;
            source2.Y = -((((source.Y - viewport.Y) / (viewport.Height)) * 2f) - 1f);
            source2.Z = (source.Z - viewport.MinDepth) / (viewport.MaxDepth - viewport.MinDepth);
            Vector3d vector = Vector3d.Transform(source2, matrix);
            double a = (((source2.X * matrix.M14) + (source2.Y * matrix.M24)) + (source2.Z * matrix.M34)) + matrix.M44;
            if (!WithinEpsilon(a, 1f))
            {
                vector.X = vector.X / a;
                vector.Y = vector.Y / a;
                vector.Z = vector.Z / a;
            }
            return vector;
        }

        public Matrix toMatrix()
        {
            return new Matrix((float)M11, (float)M12, (float)M13, (float)M14, (float)M21, (float)M22, (float)M23, (float)M24, (float)M31, (float)M32, (float)M33, (float)M34, (float)M41, (float)M42, (float)M43, (float)M44);
        }

        private static bool WithinEpsilon(double a, double b)
        {
            double num = a - b;
            return ((-1.401298E-45f <= num) && (num <= double.Epsilon));
        }

        public static Matrixd CreateTranslation(Vector3d position)
        {
            var m11 = 1;
            var m12 = 0;
            var m13 = 0;
            var m14 = 0;
            var m21 = 0;
            var m22 = 1;
            var m23 = 0;
            var m24 = 0;
            var m31 = 0;
            var m32 = 0;
            var m33 = 1;
            var m34 = 0;
            var m41 = position.X;
            var m42 = position.Y;
            var m43 = position.Z;
            var m44 = 1;
            return new Matrixd(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
        }

        public static Matrixd CreateScale(double scale)
        {
            return CreateScale(scale, scale, scale);
        }

        public static Matrixd CreateScale(double xScale, double yScale, double zScale)
        {
            var m11 = xScale;
            var m12 = 0;
            var m13 = 0;
            var m14 = 0;
            var m21 = 0;
            var m22 = yScale;
            var m23 = 0;
            var m24 = 0;
            var m31 = 0;
            var m32 = 0;
            var m33 = zScale;
            var m34 = 0;
            var m41 = 0;
            var m42 = 0;
            var m43 = 0;
            var m44 = 1;
            return new Matrixd(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
        }

        public static Matrixd Multiply(Matrixd matrix1, Matrixd matrix2)
        {
            var m11 = (((matrix1.M11 * matrix2.M11) + (matrix1.M12 * matrix2.M21)) + (matrix1.M13 * matrix2.M31)) + (matrix1.M14 * matrix2.M41);
            var m12 = (((matrix1.M11 * matrix2.M12) + (matrix1.M12 * matrix2.M22)) + (matrix1.M13 * matrix2.M32)) + (matrix1.M14 * matrix2.M42);
            var m13 = (((matrix1.M11 * matrix2.M13) + (matrix1.M12 * matrix2.M23)) + (matrix1.M13 * matrix2.M33)) + (matrix1.M14 * matrix2.M43);
            var m14 = (((matrix1.M11 * matrix2.M14) + (matrix1.M12 * matrix2.M24)) + (matrix1.M13 * matrix2.M34)) + (matrix1.M14 * matrix2.M44);
            var m21 = (((matrix1.M21 * matrix2.M11) + (matrix1.M22 * matrix2.M21)) + (matrix1.M23 * matrix2.M31)) + (matrix1.M24 * matrix2.M41);
            var m22 = (((matrix1.M21 * matrix2.M12) + (matrix1.M22 * matrix2.M22)) + (matrix1.M23 * matrix2.M32)) + (matrix1.M24 * matrix2.M42);
            var m23 = (((matrix1.M21 * matrix2.M13) + (matrix1.M22 * matrix2.M23)) + (matrix1.M23 * matrix2.M33)) + (matrix1.M24 * matrix2.M43);
            var m24 = (((matrix1.M21 * matrix2.M14) + (matrix1.M22 * matrix2.M24)) + (matrix1.M23 * matrix2.M34)) + (matrix1.M24 * matrix2.M44);
            var m31 = (((matrix1.M31 * matrix2.M11) + (matrix1.M32 * matrix2.M21)) + (matrix1.M33 * matrix2.M31)) + (matrix1.M34 * matrix2.M41);
            var m32 = (((matrix1.M31 * matrix2.M12) + (matrix1.M32 * matrix2.M22)) + (matrix1.M33 * matrix2.M32)) + (matrix1.M34 * matrix2.M42);
            var m33 = (((matrix1.M31 * matrix2.M13) + (matrix1.M32 * matrix2.M23)) + (matrix1.M33 * matrix2.M33)) + (matrix1.M34 * matrix2.M43);
            var m34 = (((matrix1.M31 * matrix2.M14) + (matrix1.M32 * matrix2.M24)) + (matrix1.M33 * matrix2.M34)) + (matrix1.M34 * matrix2.M44);
            var m41 = (((matrix1.M41 * matrix2.M11) + (matrix1.M42 * matrix2.M21)) + (matrix1.M43 * matrix2.M31)) + (matrix1.M44 * matrix2.M41);
            var m42 = (((matrix1.M41 * matrix2.M12) + (matrix1.M42 * matrix2.M22)) + (matrix1.M43 * matrix2.M32)) + (matrix1.M44 * matrix2.M42);
            var m43 = (((matrix1.M41 * matrix2.M13) + (matrix1.M42 * matrix2.M23)) + (matrix1.M43 * matrix2.M33)) + (matrix1.M44 * matrix2.M43);
            var m44 = (((matrix1.M41 * matrix2.M14) + (matrix1.M42 * matrix2.M24)) + (matrix1.M43 * matrix2.M34)) + (matrix1.M44 * matrix2.M44);
            return new Matrixd(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
        }

        internal static Matrixd CreatePerspectiveFieldOfView(double fieldOfView, double aspectRatio, double nearPlaneDistance, double farPlaneDistance)
        {
            Matrixd result = new Matrixd();
            if ((fieldOfView <= 0f) || (fieldOfView >= 3.141593f))
            {
                throw new ArgumentException("fieldOfView <= 0 or >= PI");
            }
            if (nearPlaneDistance <= 0f)
            {
                throw new ArgumentException("nearPlaneDistance <= 0");
            }
            if (farPlaneDistance <= 0f)
            {
                throw new ArgumentException("farPlaneDistance <= 0");
            }
            if (nearPlaneDistance >= farPlaneDistance)
            {
                throw new ArgumentException("nearPlaneDistance >= farPlaneDistance");
            }
            double num = 1f / Math.Tan(fieldOfView * 0.5);
            double num9 = num / aspectRatio;
            result.M11 = num9;
            result.M12 = result.M13 = result.M14 = 0;
            result.M22 = num;
            result.M21 = result.M23 = result.M24 = 0;
            result.M31 = result.M32 = 0f;
            result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M34 = -1;
            result.M41 = result.M42 = result.M44 = 0;
            result.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
            return result;
        }

        public static Matrixd CreateOrthographicOffCenter(double left, double right, double bottom, double top, double zNearPlane, double zFarPlane)
        {
            Matrixd result = new Matrixd();
            result.M11 = (2.0 / (right - left));
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = (2.0 / (top - bottom));
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = (1.0 / (zNearPlane - zFarPlane));
            result.M34 = 0.0f;
            result.M41 = ((left + right) / (left - right));
            result.M42 = ((top + bottom) / (bottom - top));
            result.M43 = (zNearPlane / (zNearPlane - zFarPlane));
            result.M44 = 1.0f;
            return result;
        }

        internal static Matrixd CreateLookAt(Vector3d cameraPosition, Vector3d cameraTarget, Vector3d cameraUpVector)
        {
            Matrixd result = new Matrixd();
            var vector = Vector3d.Normalize(cameraPosition - cameraTarget);
            var vector2 = Vector3d.Normalize(Vector3d.Cross(cameraUpVector, vector));
            var vector3 = Vector3d.Cross(vector, vector2);
            result.M11 = vector2.X;
            result.M12 = vector3.X;
            result.M13 = vector.X;
            result.M14 = 0f;
            result.M21 = vector2.Y;
            result.M22 = vector3.Y;
            result.M23 = vector.Y;
            result.M24 = 0f;
            result.M31 = vector2.Z;
            result.M32 = vector3.Z;
            result.M33 = vector.Z;
            result.M34 = 0f;
            result.M41 = -Vector3d.Dot(vector2, cameraPosition);
            result.M42 = -Vector3d.Dot(vector3, cameraPosition);
            result.M43 = -Vector3d.Dot(vector, cameraPosition);
            result.M44 = 1f;
            return result;
        }

        internal static Matrixd CreateRotationX(double radians)
        {
            var result = Matrixd.Identity();

            var val1 = Math.Cos(radians);
            var val2 = Math.Sin(radians);

            result.M22 = val1;
            result.M23 = val2;
            result.M32 = -val2;
            result.M33 = val1;
            return result;
        }

        internal static Matrixd CreateRotationY(double radians)
        {
            var result = Matrixd.Identity();

            var val1 = Math.Cos(radians);
            var val2 = Math.Sin(radians);

            result.M11 = val1;
            result.M13 = -val2;
            result.M31 = val2;
            result.M33 = val1;
            return result;
        }

        internal static Matrixd CreateRotationZ(double radians)
        {
            var result = Matrixd.Identity();

            var val1 = Math.Cos(radians);
            var val2 = Math.Sin(radians);

            result.M11 = val1;
            result.M12 = val2;
            result.M21 = -val2;
            result.M22 = val1;
            return result;
        }

        internal static Matrixd Identity()
        {
            return new Matrixd(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        }

        public static Matrixd Invert(Matrixd matrix)
        {
            Matrixd result = new Matrixd();
            double num1 = matrix.M11;
            double num2 = matrix.M12;
            double num3 = matrix.M13;
            double num4 = matrix.M14;
            double num5 = matrix.M21;
            double num6 = matrix.M22;
            double num7 = matrix.M23;
            double num8 = matrix.M24;
            double num9 = matrix.M31;
            double num10 = matrix.M32;
            double num11 = matrix.M33;
            double num12 = matrix.M34;
            double num13 = matrix.M41;
            double num14 = matrix.M42;
            double num15 = matrix.M43;
            double num16 = matrix.M44;
            double num17 = (num11 * num16 - num12 * num15);
            double num18 = (num10 * num16 - num12 * num14);
            double num19 = (num10 * num15 - num11 * num14);
            double num20 = (num9 * num16 - num12 * num13);
            double num21 = (num9 * num15 - num11 * num13);
            double num22 = (num9 * num14 - num10 * num13);
            double num23 = (num6 * num17 - num7 * num18 + num8 * num19);
            double num24 = -(num5 * num17 - num7 * num20 + num8 * num21);
            double num25 = (num5 * num18 - num6 * num20 + num8 * num22);
            double num26 = -(num5 * num19 - num6 * num21 + num7 * num22);
            double num27 = (1.0 / (num1 * num23 + num2 * num24 + num3 * num25 + num4 * num26));

            result.M11 = num23 * num27;
            result.M21 = num24 * num27;
            result.M31 = num25 * num27;
            result.M41 = num26 * num27;
            result.M12 = -(num2 * num17 - num3 * num18 + num4 * num19) * num27;
            result.M22 = (num1 * num17 - num3 * num20 + num4 * num21) * num27;
            result.M32 = -(num1 * num18 - num2 * num20 + num4 * num22) * num27;
            result.M42 = (num1 * num19 - num2 * num21 + num3 * num22) * num27;
            double num28 = (num7 * num16 - num8 * num15);
            double num29 = (num6 * num16 - num8 * num14);
            double num30 = (num6 * num15 - num7 * num14);
            double num31 = (num5 * num16 - num8 * num13);
            double num32 = (num5 * num15 - num7 * num13);
            double num33 = (num5 * num14 - num6 * num13);
            result.M13 = (num2 * num28 - num3 * num29 + num4 * num30) * num27;
            result.M23 = -(num1 * num28 - num3 * num31 + num4 * num32) * num27;
            result.M33 = (num1 * num29 - num2 * num31 + num4 * num33) * num27;
            result.M43 = -(num1 * num30 - num2 * num32 + num3 * num33) * num27;
            double num34 = (num7 * num12 - num8 * num11);
            double num35 = (num6 * num12 - num8 * num10);
            double num36 = (num6 * num11 - num7 * num10);
            double num37 = (num5 * num12 - num8 * num9);
            double num38 = (num5 * num11 - num7 * num9);
            double num39 = (num5 * num10 - num6 * num9);
            result.M14 = -(num2 * num34 - num3 * num35 + num4 * num36) * num27;
            result.M24 = (num1 * num34 - num3 * num37 + num4 * num38) * num27;
            result.M34 = -(num1 * num35 - num2 * num37 + num4 * num39) * num27;
            result.M44 = (num1 * num36 - num2 * num38 + num3 * num39) * num27;
            return result;
        }

        public static Matrixd operator *(Matrixd matrix1, Matrixd matrix2)
        {
            return Matrixd.Multiply(matrix1, matrix2);
        }
    }
}

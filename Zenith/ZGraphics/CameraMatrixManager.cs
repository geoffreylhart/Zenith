using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Zenith.MathHelpers;

namespace Zenith.ZGraphics
{
    class CameraMatrixManager
    {
        // 0 is old top-down view with 90 fov
        // 1 is view from 45 degrees with 45 fov
        // 2 is isometric at 45 degrees
        // 2 is view from 45 degrees with 80 fov (cant do 90 because infinite view)
        public static int MODE = 1;
        public static int MODE_COUNT = 4;
        //static float M_1 = (float)(Math.Sin(Math.PI / 4) / Math.Sin(Math.PI / 8));
        static float M_1 = 2.5f;
        static float M_2 = 4.5f;
        static double angle = Math.PI/4;

        internal static Matrix GetWorldView(float distance)
        {
            switch (MODE)
            {
                case 0:
                    return Matrix.CreateLookAt(new Vector3(0, -1 - distance, 0), new Vector3(0, 0, 0), Vector3.UnitZ);
                case 1:
                    distance *= M_1;
                    return Matrix.CreateLookAt(new Vector3(0, -1 - distance * (float)Math.Cos(angle), -distance * (float)Math.Sin(angle)), new Vector3(0, -1, 0), Vector3.UnitZ);
                case 2:
                    distance *= M_2;
                    return Matrix.CreateLookAt(new Vector3(0, -1 - distance * (float)Math.Cos(angle), -distance * (float)Math.Sin(angle)), new Vector3(0, -1, 0), Vector3.UnitZ);
                case 3:
                    return Matrix.CreateLookAt(new Vector3(0, -1 - distance * (float)Math.Cos(angle), -distance * (float)Math.Sin(angle)), new Vector3(0, -1, 0), Vector3.UnitZ);
            }
            throw new NotImplementedException();
        }

        internal static Matrix GetWorldProjection(float distance, float aspectRatio)
        {
            switch (MODE)
            {
                case 0:
                    return Matrix.CreatePerspectiveFieldOfView(Mathf.PI / 2, aspectRatio, distance * 0.1f, distance * 100);
                case 1:
                    distance *= M_1;
                    return Matrix.CreatePerspectiveFieldOfView(Mathf.PI / 4, aspectRatio, distance * 0.1f, distance * 100);
                case 2:
                    distance *= M_2;
                    return Matrix.CreateOrthographicOffCenter(-0.2f * distance * aspectRatio, 0.2f * distance * aspectRatio, -0.2f * distance, 0.2f * distance, distance * 0.1f, distance * 100);
                case 3:
                    return Matrix.CreatePerspectiveFieldOfView(Mathf.PI * 80 / 180, aspectRatio, distance * 0.1f, distance * 100);
            }
            throw new NotImplementedException();
        }

        internal static Matrixd GetWorldViewd(double distance)
        {
            switch (MODE)
            {
                case 0:
                    return Matrixd.CreateLookAt(new Vector3d(0, -1 - distance, 0), new Vector3d(0, 0, 0), new Vector3d(0, 0, 1));
                case 1:
                    distance *= M_1;
                    return Matrixd.CreateLookAt(new Vector3d(0, -1 - distance * Math.Cos(angle), -distance * Math.Sin(angle)), new Vector3d(0, -1, 0), new Vector3d(0, 0, 1));
                case 2:
                    distance *= M_2;
                    return Matrixd.CreateLookAt(new Vector3d(0, -1 - distance * Math.Cos(angle), -distance * Math.Sin(angle)), new Vector3d(0, -1, 0), new Vector3d(0, 0, 1));
                case 3:
                    return Matrixd.CreateLookAt(new Vector3d(0, -1 - distance * Math.Cos(angle), -distance * Math.Sin(angle)), new Vector3d(0, -1, 0), new Vector3d(0, 0, 1));
            }
            throw new NotImplementedException();
        }

        internal static Matrixd GetWorldProjectiond(double distance, double aspectRatio)
        {
            switch (MODE)
            {
                case 0:
                    return Matrixd.CreatePerspectiveFieldOfView(Math.PI / 2, aspectRatio, distance * 0.1f, distance * 100);
                case 1:
                    distance *= M_1;
                    return Matrixd.CreatePerspectiveFieldOfView(Math.PI / 4, aspectRatio, distance * 0.1f, distance * 100);
                case 2:
                    distance *= M_2;
                    return Matrixd.CreateOrthographicOffCenter(-0.2 * distance * aspectRatio, 0.2 * distance * aspectRatio, -0.2 * distance, 0.2 * distance, distance * 0.1, distance * 100);
                case 3:
                    return Matrixd.CreatePerspectiveFieldOfView(Math.PI * 80 / 180, aspectRatio, distance * 0.1f, distance * 100);
            }
            throw new NotImplementedException();
        }

        internal static Matrix GetWorldRelativeView(float distance)
        {
            switch (MODE)
            {
                case 0:
                    return Matrix.CreateLookAt(new Vector3(0, -distance, 0), new Vector3(0, 0, 0), Vector3.UnitZ); // TODO: this is hacky
                case 1:
                    distance *= M_1;
                    return Matrix.CreateLookAt(new Vector3(0, -distance * (float)Math.Cos(angle), -distance * (float)Math.Sin(angle)), new Vector3(0, 0, 0), Vector3.UnitZ); // TODO: this is hacky
                case 2:
                    distance *= M_2;
                    return Matrix.CreateLookAt(new Vector3(0, -distance * (float)Math.Cos(angle), -distance * (float)Math.Sin(angle)), new Vector3(0, 0, 0), Vector3.UnitZ); // TODO: this is hacky
                case 3:
                    return Matrix.CreateLookAt(new Vector3(0, -distance * (float)Math.Cos(angle), -distance * (float)Math.Sin(angle)), new Vector3(0, 0, 0), Vector3.UnitZ); // TODO: this is hacky
            }
            throw new NotImplementedException();
        }

        internal static Matrix GetShipProjection(float aspectRatio, float width, float height)
        {
            switch (MODE)
            {
                case 0:
                    return Matrix.CreatePerspectiveFieldOfView(Mathf.PI / 2, aspectRatio, 0.1f, 100);
                case 1:
                    return Matrix.CreatePerspectiveFieldOfView(Mathf.PI / 4, aspectRatio, 0.1f, 1000);
                case 2:
                    float pixelsPerUnit = 32;
                    width = aspectRatio * 1200;
                    height = 1200;
                    return Matrix.CreateOrthographicOffCenter(-width / pixelsPerUnit / 2, width / pixelsPerUnit / 2, -height / pixelsPerUnit / 2, height / pixelsPerUnit / 2, 1, 1000); // note: we've flipped this from usual to match blender coordinates?
                case 3:
                    return Matrix.CreatePerspectiveFieldOfView(Mathf.PI * 80 / 180, aspectRatio, 0.1f, 1000);
            }
            throw new NotImplementedException();
        }

        internal static Matrix GetShipView()
        {
            switch (MODE)
            {
                case 0:
                    return Matrix.CreateLookAt(new Vector3(0, 0, 20), new Vector3(0, 0, 0), -Vector3.UnitY);
                case 1:
                    return Matrix.CreateLookAt(new Vector3(0, 20 * M_1 * (float)Math.Sin(angle), 20 * M_1 * (float)Math.Cos(angle)), new Vector3(0, 0, 0), -Vector3.UnitY);
                case 2:
                    return Matrix.CreateLookAt(new Vector3(0, 20 * M_2 * (float)Math.Sin(angle), 20 * M_2 * (float)Math.Cos(angle)), new Vector3(0, 0, 0), -Vector3.UnitY);
                case 3:
                    return Matrix.CreateLookAt(new Vector3(0, 20 * M_1 * (float)Math.Sin(angle), 20 * M_1 * (float)Math.Cos(angle)), new Vector3(0, 0, 0), -Vector3.UnitY);
            }
            throw new NotImplementedException();
        }
    }
}

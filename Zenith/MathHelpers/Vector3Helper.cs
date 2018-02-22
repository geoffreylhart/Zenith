using System;
using Microsoft.Xna.Framework;

namespace Zenith.MathHelpers
{
    public static class Vector3Helper
    {
        internal static Vector3 UnitSphere(double latitude, double longitude)
        {
            double dxy = Math.Cos(latitude);
            double x = Math.Cos(longitude) * dxy;
            double y = Math.Sin(longitude) * dxy;
            double z = -Math.Sin(latitude);
            return new Vector3((float)x, (float)y, (float)z);
        }
    }
}

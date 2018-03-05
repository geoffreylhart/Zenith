using System;
using Microsoft.Xna.Framework;

namespace Zenith.MathHelpers
{
    public static class Vector3Helper
    {
        internal static Vector3 UnitSphere(double longitude, double latitude)
        {
            double dz = Math.Sin(latitude);
            double dxy = Math.Cos(latitude); // the radius of the horizontal ring section, always positive
            double dx = Math.Sin(longitude) * dxy;
            double dy = -Math.Cos(longitude) * dxy;
            return new Vector3((float)dx, (float)dy, (float)dz);
        }
    }
}

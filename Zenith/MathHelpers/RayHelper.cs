using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.MathHelpers
{
    internal static class RayHelper
    {
        // looks like the graphicsDevice viewport is mostly used for screen resolution and stuff? I guess that makes sense
        internal static Ray CastFromCamera(GraphicsDevice graphicsDevice, Vector3 cameraPosition, Vector2 screenCoordinate, Matrix projection, Matrix view, Matrix world)
        {
            Vector3 unprojected = graphicsDevice.Viewport.Unproject(new Vector3(screenCoordinate.X, screenCoordinate.Y, 1), projection, view, world);
            return new Ray(cameraPosition, unprojected - cameraPosition);
        }

        internal static Vector3? IntersectionPoint(this Ray ray, Plane plane)
        {
            float? intersectionF = ray.Intersects(plane);
            if (!intersectionF.HasValue) return null;
            return ray.Position + intersectionF.Value * ray.Direction;
        }

        // seems like this isn't what we thought it was. But what is it then??
        internal static Vector3? IntersectionPoint(this Ray ray, BoundingSphere sphere)
        {
            float? intersectionF = ray.Intersects(sphere);
            if (!intersectionF.HasValue) return null;
            return ray.Position + intersectionF.Value * ray.Direction;
        }

        internal static Vector3? IntersectionSphere(this Ray ray, BoundingSphere sphere)
        {
            // just wikied sphere intersection math
            Vector3 v_2 = ray.Direction / ray.Direction.Length();
            float t_1 = -Vector3.Dot(v_2, ray.Position - sphere.Center);
            float t_2 = (float)Math.Sqrt(Math.Pow(t_1, 2) - (ray.Position - sphere.Center).LengthSquared() + sphere.Radius * sphere.Radius);
            float d = t_1 - t_2 >= 0 ? t_1 - t_2 : t_1 + t_2; // return the smallest legal time
            Vector3 finalPos = ray.Position + v_2 * d;
            return finalPos;
        }
    }
}

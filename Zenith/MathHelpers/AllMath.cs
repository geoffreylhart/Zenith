using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

// a collection of lots of inline math methods, similar to the System.Math library
// mostly intended to be used with built-in classes. and perhaps some simple double versions of built-in classes
// or maybe, just always include built-in versions if you're going to have a non built-in version
// I'm tired of redoing all this math so PLEASE make it as reuseable as possible
namespace Zenith.MathHelpers
{
    public class AllMath
    {
        public static float DistanceFromLineOrPoints(Vector2 p, Vector2 l1, Vector2 l2)
        {
            float t = Vector2.Dot(l2 - l1, p - l1) / (l2 - l1).LengthSquared();
            if (t < 0) return (p - l1).Length();
            if (t > 1) return (p - l2).Length();
            return (p - (l1 + (l2 - l1) * t)).Length();
        }

        public static Vector2 ClosestPointOnLineOrPoints(Vector2 p, Vector2 l1, Vector2 l2)
        {
            float t = Vector2.Dot(l2 - l1, p - l1) / (l2 - l1).LengthSquared();
            t = Math.Min(1, Math.Max(0, t));
            return l1 + (l2 - l1) * t;
        }

        public static float ProjectionTOnLine(Vector2 p, Vector2 l1, Vector2 l2)
        {
            return Vector2.Dot(l2 - l1, p - l1) / (l2 - l1).LengthSquared();
        }

        public static Vector2 GetMouseLongLat()
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Helpers;

namespace Zenith.MathHelpers
{
    public class Rayd
    {
        public Vector3d Position;
        public Vector3d Direction;

        public Rayd(Vector3d position, Vector3d direction)
        {
            this.Position = position;
            this.Direction = direction;
        }

        // more accurate version
        internal static Rayd CastFromCamera(GraphicsDevice graphicsDevice, double x, double y, Matrixd projection, Matrixd view, Matrixd world)
        {
            // close enough to the camera position for me, I think it's a position on the near clip plane
            Vector3d unprojected = Matrixd.Unproject(graphicsDevice.Viewport, new Vector3d(x, y, 0), projection, view, world);
            Vector3d unprojected2 = Matrixd.Unproject(graphicsDevice.Viewport, new Vector3d(x, y, 1), projection, view, world);
            return new Rayd(unprojected, unprojected2 - unprojected);
        }

        internal Vector3d IntersectionSphere(Vector3d sphereCenter, double sphereRadius)
        {
            // just wikied sphere intersection math
            Vector3d v_2 = this.Direction / this.Direction.Length();
            double t_1 = -Vector3d.Dot(v_2, this.Position - sphereCenter);
            double t_2 = (float)Math.Sqrt(Math.Pow(t_1, 2) - (this.Position - sphereCenter).LengthSquared() + sphereRadius * sphereRadius);
            if (double.IsNaN(t_2) || t_1 + t_2 < 0) return null;
            double d = t_1 - t_2 >= 0 ? t_1 - t_2 : t_1 + t_2; // return the smallest legal time
            //d = t_1 - t_2;
            Vector3d finalPos = this.Position + v_2 * d;
            return finalPos;
        }
    }
}

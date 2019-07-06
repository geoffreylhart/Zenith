using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Zenith.MathHelpers;

namespace Zenith.ZMath
{
    public interface ISector
    {
        int Zoom { get; set; }
        double ZoomPortion { get; }
        int X { get; set; }
        int Y { get; set; }
        double SurfaceAreaPortion { get; }

        List<ISector> GetChildrenAtLevel(int z);
        List<ISector> GetAllParents();
        bool ContainsLongLat(LongLat longLat);
        int GetRelativeXOf(ISector sector);
        int GetRelativeYOf(ISector sector);
        LongLat[] GetIntersections(LongLat ll1, LongLat ll2);

        // all coordinates given between 0 - 1
        List<ISector> GetSectorsInRange(double minX, double maxX, double minY, double maxY, int zoom);

        Vector2d ProjectToLocalCoordinates(Vector3d v);
        Vector3d ProjectToSphereCoordinates(Vector2d v);
        ISector GetSectorAt(double x, double y, int zoom);
        ISector GetRoot();
    }
}

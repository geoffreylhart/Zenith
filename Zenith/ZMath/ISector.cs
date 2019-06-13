using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.ZMath
{
    public interface ISector
    {
        int Zoom { get; set; }
        int X { get; set; }
        int Y { get; set; }
        double SurfaceAreaPortion { get; }
        LongLat TopLeftCorner { get; }
        LongLat TopRightCorner { get; }
        LongLat BottomLeftCorner { get; }
        LongLat BottomRightCorner { get; }
        double Longitude { get; }
        double Latitude { get; }

        List<ISector> GetChildrenAtLevel(int z);
        List<ISector> GetAllParents();
        bool ContainsLongLat(LongLat longLat);
        int GetRelativeXOf(ISector sector);
        int GetRelativeYOf(ISector sector);
        LongLat[] GetIntersections(LongLat ll1, LongLat ll2);
    }
}

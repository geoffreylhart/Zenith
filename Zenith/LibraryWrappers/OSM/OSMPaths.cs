using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    class OSMPaths
    {
        public static string GetSectorPath(MercatorSector sector)
        {
            MercatorSector parent = sector.GetChildrenAtLevel(sector.zoom + 1)[0].GetAllParents().Where(x => x.zoom == 10).Single();
            MercatorSector parent5 = sector.GetChildrenAtLevel(sector.zoom + 1)[0].GetAllParents().Where(x => x.zoom == 5).Single();
            return @"..\..\..\..\LocalCache\OpenStreetMaps\" + parent5.ToString() + "\\" + parent.ToString() + ".osm.pbf";
        }
    }
}

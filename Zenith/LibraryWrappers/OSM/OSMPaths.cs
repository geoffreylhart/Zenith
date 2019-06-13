using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    class OSMPaths
    {
        public static string GetSectorPath(ISector sector)
        {
            if (sector is MercatorSector)
            {
                ISector parent = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 10).Single();
                ISector parent5 = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 5).Single();
                return @"..\..\..\..\LocalCache\OpenStreetMaps\" + parent5.ToString() + "\\" + parent.ToString() + ".osm.pbf";
            }
            if (sector is CubeSector)
            {
                ISector parent = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 8).Single();
                ISector parent4 = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 4).Single();
                return @"..\..\..\..\LocalCache\OpenStreetMaps\" + ((CubeSector)sector).sectorFace.GetFaceAcronym() + "Face\\" + parent4.ToString() + "\\" + parent.ToString() + ".osm.pbf";
            }
            throw new NotImplementedException();
        }

        public static string GetPlanetPath()
        {
            return @"..\..\..\..\LocalCache\planet-latest.osm.pbf";
        }
    }
}

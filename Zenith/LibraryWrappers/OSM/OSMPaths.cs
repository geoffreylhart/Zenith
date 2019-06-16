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
                var parent = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 10);
                var parent5 = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 5);
                if (parent.Count() != 0)
                {
                    return @"..\..\..\..\LocalCache\OpenStreetMaps\" + parent5.Single().ToString() + "\\" + parent.Single().ToString() + ".osm.pbf";
                }
                if (parent5.Count() != 0)
                {
                    return @"..\..\..\..\LocalCache\OpenStreetMaps\" + parent5.Single().ToString() + "\\" + sector.ToString() + ".osm.pbf";
                }
                return @"..\..\..\..\LocalCache\OpenStreetMaps\" + sector.ToString() + ".osm.pbf";
            }
            if (sector is CubeSector)
            {
                var parent = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 8);
                var parent4 = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 4);
                if (parent.Count() != 0)
                {
                    return @"..\..\..\..\LocalCache\OpenStreetMaps\" + ((CubeSector)sector).sectorFace.GetFaceAcronym() + "Face\\" + parent4.Single().ToString() + "\\" + parent.Single().ToString() + ".osm.pbf";
                }
                if (parent4.Count() != 0)
                {
                    return @"..\..\..\..\LocalCache\OpenStreetMaps\" + ((CubeSector)sector).sectorFace.GetFaceAcronym() + "Face\\" + parent4.Single().ToString() + "\\" + sector.ToString() + ".osm.pbf";
                }
                return @"..\..\..\..\LocalCache\OpenStreetMaps\" + ((CubeSector)sector).sectorFace.GetFaceAcronym() + "Face\\" + sector.ToString() + ".osm.pbf";
            }
            throw new NotImplementedException();
        }

        public static string GetPlanetPath()
        {
            return @"..\..\..\..\LocalCache\planet-latest.osm.pbf";
        }

        public static string GetPlanetStepPath()
        {
            return @"..\..\..\..\LocalCache\OpenStreetMaps\planet-step.txt";
        }
    }
}

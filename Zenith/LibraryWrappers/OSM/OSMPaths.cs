using System;
using System.Collections.Generic;
using System.IO;
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
                    return Path.Combine(GetOpenStreetMapsRoot(), parent5.Single().ToString(), parent.Single().ToString() + ".osm.pbf");
                }
                if (parent5.Count() != 0)
                {
                    return Path.Combine(GetOpenStreetMapsRoot(), parent5.Single().ToString(), sector.ToString() + ".osm.pbf");
                }
                return Path.Combine(GetOpenStreetMapsRoot(), sector.ToString() + ".osm.pbf");
            }
            if (sector is CubeSector)
            {
                var parent = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 8);
                var parent4 = sector.GetChildrenAtLevel(sector.Zoom + 1)[0].GetAllParents().Where(x => x.Zoom == 4);
                if (parent.Count() != 0)
                {
                    return Path.Combine(GetOpenStreetMapsRoot(), ((CubeSector)sector).sectorFace.GetFaceAcronym() + "Face", parent4.Single().ToString(), parent.Single().ToString() + ".osm.pbf");
                }
                if (parent4.Count() != 0)
                {
                    return Path.Combine(GetOpenStreetMapsRoot(), ((CubeSector)sector).sectorFace.GetFaceAcronym() + "Face", parent4.Single().ToString(), sector.ToString() + ".osm.pbf");
                }
                return Path.Combine(GetOpenStreetMapsRoot(), ((CubeSector)sector).sectorFace.GetFaceAcronym() + "Face", sector.ToString() + ".osm.pbf");
            }
            throw new NotImplementedException();
        }

        public static string GetOpenStreetMapsRoot()
        {
            return @"..\..\..\..\LocalCache\OpenStreetMapsOld";
        }

        public static string GetRenderRoot()
        {
            return Path.Combine(GetOpenStreetMapsRoot(), "Renders");
        }

        public static string GetPlanetPath()
        {
            return @"..\..\..\..\LocalCache\planet-latest.osm.pbf";
        }

        public static string GetPlanetStepPath()
        {
            return Path.Combine(GetOpenStreetMapsRoot(), "planet-step.txt");
        }

        internal static string GetCoastlineImagePath(ISector sector)
        {
            if (sector is MercatorSector)
            {
                return Path.Combine(GetRenderRoot(), "Coastline.PNG");
            }
            if (sector is CubeSector)
            {
                return Path.Combine(GetRenderRoot(), $"Coastline{((CubeSector)sector).sectorFace.GetFaceAcronym()}.PNG");
            }
            throw new NotImplementedException();
        }
    }
}

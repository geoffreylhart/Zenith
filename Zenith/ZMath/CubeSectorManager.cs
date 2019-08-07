using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZGeom;

namespace Zenith.ZMath
{
    public class CubeSectorManager : ISectorManager
    {
        public List<ISector> GetTopmostOSMSectors()
        {
            List<ISector> sectors = new List<ISector>();
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.FRONT, 0, 0, 0));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.BACK, 0, 0, 0));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.LEFT, 0, 0, 0));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.RIGHT, 0, 0, 0));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.TOP, 0, 0, 0));
            sectors.Add(new CubeSector(CubeSector.CubeSectorFace.BOTTOM, 0, 0, 0));
            return sectors;
        }

        public int GetHighestOSMZoom()
        {
            // at 8 for CubeSector, we expect 50% more filespace spent on images
            return 8;
        }

        public ISector FromString(string s)
        {
            String[] split = s.Split(',');
            CubeSector.CubeSectorFace sectorFace = ZCoords.FromFaceAcronym(split[0]);
            int x = int.Parse(split[1].Split('=')[1]);
            int y = int.Parse(split[2].Split('=')[1]);
            int zoom = int.Parse(split[3].Split('=', '.')[1]);
            return new CubeSector(sectorFace, x, y, zoom);
        }

        public int GetHighestCacheZoom()
        {
            return 7;
        }
    }
}

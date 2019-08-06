using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.ZMath
{
    public class MercatorSectorManager : ISectorManager
    {
        public List<ISector> GetTopmostOSMSectors()
        {
            List<ISector> sectors = new List<ISector>();
            sectors.Add(new MercatorSector(0, 0, 0));
            return sectors;
        }

        public int GetHighestOSMZoom()
        {
            return 10;
        }

        public ISector FromString(string s)
        {
            String[] split = s.Split(',');
            int x = int.Parse(split[0].Split('=')[1]);
            int y = int.Parse(split[1].Split('=')[1]);
            int zoom = int.Parse(split[2].Split('=')[1]);
            return new MercatorSector(x, y, zoom);
        }

        public int GetHighestCacheZoom()
        {
            return 7;
        }
    }
}

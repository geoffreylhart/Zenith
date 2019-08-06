using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.ZMath
{
    public interface ISectorManager
    {
        List<ISector> GetTopmostOSMSectors();

        int GetHighestOSMZoom();
        ISector FromString(string s);
        int GetHighestCacheZoom();
    }
}

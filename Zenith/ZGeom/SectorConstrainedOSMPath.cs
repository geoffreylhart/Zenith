using System;
using System.Collections.Generic;
using System.Text;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    public class SectorConstrainedOSMPath
    {
        public Vector2d start = null;
        public List<long> nodeRefs = new List<long>();
        public Vector2d end = null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    public class SectorConstrainedOSMAreaMap
    {
        public List<SectorConstrainedOSMPath> paths = new List<SectorConstrainedOSMPath>();
        public List<List<long>> inners = new List<List<long>>(); // holes
        public List<List<long>> outers = new List<List<long>>(); // islands

        internal SectorConstrainedOSMAreaMap Subtract(SectorConstrainedOSMAreaMap areaMap)
        {
            throw new NotImplementedException();
        }

        internal SectorConstrainedAreaMap Finalize(BlobCollection blobs)
        {
            SectorConstrainedAreaMap map = new SectorConstrainedAreaMap();
            map.inners = inners.Select(x => x.Select(y => blobs.nodes[y]).ToList()).ToList();
            map.outers = outers.Select(x => x.Select(y => blobs.nodes[y]).ToList()).ToList();
            foreach (var path in paths)
            {
                List<Vector2d> newPath = new List<Vector2d>();
                newPath.Add(path.start);
                newPath.AddRange(path.nodeRefs.Select(x => blobs.nodes[x]));
                newPath.Add(path.end);
                map.paths.Add(newPath);
            }
            return map;
        }
    }
}

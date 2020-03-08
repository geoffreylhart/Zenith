using System;
using System.Collections.Generic;
using System.Text;
using Zenith.ZGeom;
using Zenith.ZMath;
using static Zenith.LibraryWrappers.OSM.OSMMetaManager;

namespace Zenith.LibraryWrappers.OSM
{
    // meant to be a more compressed version of OSMMetaManager (no extra data)
    // TODO: just occurred to me that none of this can handle malformed relations that dont even form a complete shape
    // TODO: also just occurred to me that maybe our way inits shouldn't use a dictionary, since there could be duplicates??
    class OSMMetaFinal
    {
        public static List<string> naturalTypes = new List<string>() {
            "coastline", "water", "wood", "scrub", "heath", "moor", "grassland", "fell", "bare_rock", "scree",
            "shingle", "sand", "mud", "wetland", "glacier", "bay", "strait", "beach", "reef", "dune",
            "cliff", "isthmus", "peninsula", "rock", "sinkhole", "cave_entrance", "desert"
        };

        Dictionary<ISector, GridPointInfo[,]> gridPoints; // the final actual info
        Dictionary<ISector, GridPointInfo[,]> gridTops; // intermediate
        Dictionary<ISector, GridPointInfo[,]> gridLefts; // intermediate

        internal void LoadAll(string fileName)
        {
            OSMMetaManager manager = new OSMMetaManager();
            manager.LoadAll(fileName);
            // init
            gridPoints = new Dictionary<ISector, GridPointInfo[,]>();
            gridTops = new Dictionary<ISector, GridPointInfo[,]>();
            gridLefts = new Dictionary<ISector, GridPointInfo[,]>();
            foreach (var root in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                GridPointInfo[,] gp = new GridPointInfo[257, 257];
                GridPointInfo[,] gt = new GridPointInfo[256, 257];
                GridPointInfo[,] gl = new GridPointInfo[257, 256];
                for (int x = 0; x < 257; x++)
                {
                    for (int y = 0; y < 257; y++)
                    {
                        gp[x, y] = new GridPointInfo();
                        if (x < 256) gt[x, y] = new GridPointInfo();
                        if (y < 256) gl[x, y] = new GridPointInfo();
                    }
                }
                gridPoints[root] = gp;
                gridTops[root] = gt;
                gridLefts[root] = gl;
            }
            foreach (var edge in manager.edgeInfo)
            {
                // coastline only, to start with
                if (manager.wayInfo[edge.wayID].keyValues.ContainsKey("natural") && manager.wayInfo[edge.wayID].keyValues["natural"].Equals("coastline"))
                {
                    foreach (var root in ZCoords.GetSectorManager().GetTopmostOSMSectors())
                    {
                        var local1 = root.ProjectToLocalCoordinates(edge.longLat1.ToSphereVector());
                        var local2 = root.ProjectToLocalCoordinates(edge.longLat1.ToSphereVector());
                        // first, order by X
                        if (local1.X > local2.X)
                        {
                            var temp = local2;
                            local2 = local1;
                            local1 = temp;
                        }
                        for (int x = (int)Math.Ceiling(local1.X * 256); x < local2.X * 256; x++)
                        {
                            double t = (x - local1.X * 256) / (local2.X - local1.X);
                            int y = (int)(local1.Y + t * (local2.Y - local1.Y));
                            if (x >= 0 && x < 257 && y >= 0 && y < 256)
                            {
                                gridLefts[root][x, y].naturalTypes.Add(0);
                            }
                        }
                        // now, order by Y
                        if (local1.Y > local2.Y)
                        {
                            var temp = local2;
                            local2 = local1;
                            local1 = temp;
                        }
                        for (int y = (int)Math.Ceiling(local1.Y * 256); y < local2.Y * 256; y++)
                        {
                            double t = (y - local1.Y * 256) / (local2.Y - local1.Y);
                            int x = (int)(local1.X + t * (local2.X - local1.X));
                            if (x >= 0 && x < 256 && y >= 0 && y < 257)
                            {
                                gridTops[root][x, y].naturalTypes.Add(0);
                            }
                        }
                    }
                }
            }
        }

        // as a point, represents the land types and relations that contain it
        // as an edge, represents the same states that will be added or removed upon crossing
        public class GridPointInfo
        {
            public HashSet<long> relations;
            public HashSet<int> naturalTypes; // by index
        }
    }
}

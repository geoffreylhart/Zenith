using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
            if (fileName.Contains("*"))
            {
                for (int i = 0; i < 6; i++)
                {
                    manager.LoadAll(fileName.Replace("*", i + ""));
                }
            }
            else
            {
                manager.LoadAll(fileName);
            }
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
            // process all edge info
            foreach (var edge in manager.edgeInfo)
            {
                var way = manager.wayInfo[edge.wayID];
                if ((edge.node1 == way.startNode && edge.node2 == way.endNode) || (edge.node1 == way.endNode && edge.node2 == way.startNode)) continue; // for coast, let's reject all simple shape closures
                // coastline only, to start with
                if (manager.wayInfo[edge.wayID].keyValues.ContainsKey("natural") && manager.wayInfo[edge.wayID].keyValues["natural"].Equals("coastline"))
                {
                    foreach (var root in ZCoords.GetSectorManager().GetTopmostOSMSectors())
                    {
                        var local1 = root.ProjectToLocalCoordinates(edge.longLat1.ToSphereVector());
                        var local2 = root.ProjectToLocalCoordinates(edge.longLat2.ToSphereVector());
                        if ((local1 - local2).Length() > 0.2) continue;
                        // first, order by X
                        if (local1.X > local2.X)
                        {
                            var temp = local2;
                            local2 = local1;
                            local1 = temp;
                        }
                        for (int x = (int)Math.Ceiling(local1.X * 256); x < local2.X * 256; x++)
                        {
                            double t = (x / 256.0 - local1.X) / (local2.X - local1.X);
                            int y = (int)((local1.Y + t * (local2.Y - local1.Y)) * 256);
                            if (x >= 0 && x < 257 && y >= 0 && y < 256)
                            {
                                if (gridLefts[root][x, y].naturalTypes.Contains(0))
                                {
                                    gridLefts[root][x, y].naturalTypes.Remove(0);
                                }
                                else
                                {
                                    gridLefts[root][x, y].naturalTypes.Add(0);
                                }
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
                            double t = (y / 256.0 - local1.Y) / (local2.Y - local1.Y);
                            int x = (int)((local1.X + t * (local2.X - local1.X)) * 256);
                            if (x >= 0 && x < 256 && y >= 0 && y < 257)
                            {
                                if (gridTops[root][x, y].naturalTypes.Contains(0))
                                {
                                    gridTops[root][x, y].naturalTypes.Remove(0);
                                }
                                else
                                {
                                    gridTops[root][x, y].naturalTypes.Add(0);
                                }
                            }
                        }
                    }
                }
            }
            // now actually figure out the points
            // TODO: we're just doing front for now
            var frRoot = new CubeSector(CubeSector.CubeSectorFace.FRONT, 0, 0, 0);
            for (int y = 0; y < 257; y++)
            {
                for (int x = 0; x < 257; x++)
                {
                    GridPointInfo prev;
                    GridPointInfo next;
                    GridPointInfo edge;
                    if (x == 0)
                    {
                        if (y == 0) continue;
                        prev = gridPoints[frRoot][0, y - 1];
                        next = gridPoints[frRoot][0, y];
                        edge = gridLefts[frRoot][0, y - 1];
                    }
                    else
                    {
                        prev = gridPoints[frRoot][x - 1, y];
                        next = gridPoints[frRoot][x, y];
                        edge = gridTops[frRoot][x - 1, y];
                    }
                    foreach (var n in prev.naturalTypes) next.naturalTypes.Add(n);
                    foreach (var n in edge.naturalTypes)
                    {
                        if (next.naturalTypes.Contains(n))
                        {
                            next.naturalTypes.Remove(n);
                        }
                        else
                        {
                            next.naturalTypes.Add(n);
                        }
                    }
                }
            }
            // finally, render those coast images
            string mapFile = OSMPaths.GetCoastlineImagePath(frRoot);
            Bitmap map = new Bitmap(256, 256);
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    bool land1 = gridPoints[frRoot][i, j].naturalTypes.Contains(0);
                    bool land2 = gridPoints[frRoot][i + 1, j].naturalTypes.Contains(0);
                    bool land3 = gridPoints[frRoot][i, j + 1].naturalTypes.Contains(0);
                    bool land4 = gridPoints[frRoot][i + 1, j + 1].naturalTypes.Contains(0);
                    Color color = Color.FromArgb(128, 128, 128);
                    if (land1 && land2 && land3 && land4) color = Color.FromArgb(0, 255, 0);
                    if (!land1 && !land2 && !land3 && !land4) color = Color.FromArgb(255, 255, 255);

                    //color = Color.FromArgb(255, 255, 255);
                    //if (gridTops[frRoot][i,j].naturalTypes.Contains(0) && !(land1 && land2 && land3 && land4) && !(!land1 && !land2 && !land3 && !land4)) color = Color.FromArgb(0, 255, 0);

                    map.SetPixel(i, j, color);
                }
            }
            map.Save(mapFile, ImageFormat.Png);
        }

        // as a point, represents the land types and relations that contain it
        // as an edge, represents the same states that will be added or removed upon crossing
        public class GridPointInfo
        {
            public HashSet<long> relations = new HashSet<long>();
            public HashSet<int> naturalTypes = new HashSet<int>(); // by index
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public HashSet<long> badRelations = new HashSet<long>();

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
            // find bad relations
            foreach (var relation in manager.relationInfo)
            {
                if (!IsValidRelation(manager, relation.Key)) badRelations.Add(relation.Key);
            }
            // process all edge info
            foreach (var edge in manager.edgeInfo)
            {
                if (!manager.wayInfo.ContainsKey(edge.wayID)) continue; // probably doesn't exist because we've removed it to try and save some memory
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
                    for (int x = (int)Math.Ceiling(local1.X * 256); x <= local2.X * 256; x++)
                    {
                        if (x == local1.X * 256) continue;
                        double t = (x / 256.0 - local1.X) / (local2.X - local1.X);
                        int y = (int)Math.Floor((local1.Y + t * (local2.Y - local1.Y)) * 256);
                        if (x >= 0 && x < 257 && y >= 0 && y < 256)
                        {
                            XORWithEdge(manager, gridLefts[root][x, y], edge);
                        }
                    }
                    // now, order by Y
                    if (local1.Y > local2.Y)
                    {
                        var temp = local2;
                        local2 = local1;
                        local1 = temp;
                    }
                    for (int y = (int)Math.Ceiling(local1.Y * 256); y <= local2.Y * 256; y++) // BUG: can't believe this was required, but we do sometimes have nodes on exactly the edge, apparently
                    {
                        // ignore the edge that bruhes up exactly against the top (assuming it exists at all, since our original load logic can exclude such an edge)
                        // with a point exactly on an edge, the -other- edge that matches exactly on bottom should trigger the flag instead (double-flag would be bad)
                        if (y == local1.Y * 256) continue;
                        double t = (y / 256.0 - local1.Y) / (local2.Y - local1.Y);
                        int x = (int)Math.Floor((local1.X + t * (local2.X - local1.X)) * 256); // BUG: (int) is NOT THE SAME AS Math.Floor!
                        if (x >= 0 && x < 256 && y >= 0 && y < 257)
                        {
                            XORWithEdge(manager, gridTops[root][x, y], edge);
                        }
                    }
                }
            }
            // now actually figure out the points
            // TODO: we're just doing front for now
            var frRoot = new CubeSector((CubeSector.CubeSectorFace)int.Parse(Regex.Match(fileName, "[0-9]").Value), 0, 0, 0);
            if (frRoot.sectorFace == CubeSector.CubeSectorFace.RIGHT || frRoot.sectorFace == CubeSector.CubeSectorFace.BACK)
            {
                // invert these faces
                gridPoints[frRoot][0, 0].naturalTypes.Add(0);
            }
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
                    foreach (var n in prev.relations) next.relations.Add(n);
                    foreach (var n in prev.ways) next.ways.Add(n);
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
                    foreach (var n in edge.relations)
                    {
                        if (next.relations.Contains(n))
                        {
                            next.relations.Remove(n);
                        }
                        else
                        {
                            next.relations.Add(n);
                        }
                    }
                    foreach (var n in edge.ways)
                    {
                        if (next.ways.Contains(n))
                        {
                            next.ways.Remove(n);
                        }
                        else
                        {
                            next.ways.Add(n);
                        }
                    }
                }
            }
            SaveAsImage(manager, frRoot);
            SaveAsFile(manager, frRoot);
        }

        private void SaveAsImage(OSMMetaManager manager, CubeSector frRoot)
        {
            // finally, render those coast images
            string mapFile = OSMPaths.GetCoastlineImagePath(frRoot);
            Bitmap map = new Bitmap(256, 256);
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    int land1 = gridPoints[frRoot][i, j].naturalTypes.Contains(0) ? 0 : -1;
                    int land2 = gridPoints[frRoot][i + 1, j].naturalTypes.Contains(0) ? 0 : -1;
                    int land3 = gridPoints[frRoot][i, j + 1].naturalTypes.Contains(0) ? 0 : -1;
                    int land4 = gridPoints[frRoot][i + 1, j + 1].naturalTypes.Contains(0) ? 0 : -1;
                    if (gridPoints[frRoot][i, j].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "water"))) land1 = 1;
                    if (gridPoints[frRoot][i, j].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "water"))) land1 = 1;
                    if (gridPoints[frRoot][i + 1, j].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "water"))) land2 = 1;
                    if (gridPoints[frRoot][i + 1, j].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "water"))) land2 = 1;
                    if (gridPoints[frRoot][i, j + 1].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "water"))) land3 = 1;
                    if (gridPoints[frRoot][i, j + 1].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "water"))) land3 = 1;
                    if (gridPoints[frRoot][i + 1, j + 1].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "water"))) land4 = 1;
                    if (gridPoints[frRoot][i + 1, j + 1].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "water"))) land4 = 1;
                    if (gridPoints[frRoot][i, j].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land1 = 2;
                    if (gridPoints[frRoot][i, j].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land1 = 2;
                    if (gridPoints[frRoot][i + 1, j].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land2 = 2;
                    if (gridPoints[frRoot][i + 1, j].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land2 = 2;
                    if (gridPoints[frRoot][i, j + 1].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land3 = 2;
                    if (gridPoints[frRoot][i, j + 1].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land3 = 2;
                    if (gridPoints[frRoot][i + 1, j + 1].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land4 = 2;
                    if (gridPoints[frRoot][i + 1, j + 1].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", "glacier"))) land4 = 2;
                    Color color = Color.FromArgb(128, 128, 128);

                    if (land1 == 2 && land2 == 2 && land3 == 2 && land4 == 2) color = Color.FromArgb(255, 255, 255);
                    if (land1 == 1 && land2 == 1 && land3 == 1 && land4 == 1) color = Color.FromArgb(0, 0, 255);
                    if (land1 == 0 && land2 == 0 && land3 == 0 && land4 == 0) color = Color.FromArgb(0, 255, 0);
                    if (land1 == -1 && land2 == -1 && land3 == -1 && land4 == -1) color = Color.FromArgb(0, 0, 255);

                    //color = Color.FromArgb(255, 255, 255);
                    //if (gridTops[frRoot][i, j].relations.Any(x => x == 1279614)) color = Color.FromArgb(0, 255, 0);
                    //if (gridLefts[frRoot][i, j].relations.Any(x => x == 1279614)) color = Color.FromArgb(255, 0, 0);
                    //if (gridLefts[frRoot][i, j].relations.Any(x => x == 1279614) && gridTops[frRoot][i, j].relations.Any(x => x == 1279614)) color = Color.FromArgb(0, 0, 0);

                    map.SetPixel(i, j, color);
                }
            }
            map.Save(mapFile, ImageFormat.Png);
        }

        private void SaveAsFile(OSMMetaManager manager, CubeSector root)
        {
            for (int i = 0; i < 257; i++)
            {
                for (int j = 0; j < 257; j++)
                {
                    for (int k = 1; k < naturalTypes.Count; k++)
                    {
                        bool containsThisType = false;
                        containsThisType |= gridPoints[root][i, j].relations.Any(x => manager.relationInfo[x].ContainsKeyValue(manager, "natural", naturalTypes[k]));
                        containsThisType |= gridPoints[root][i, j].ways.Any(x => manager.wayInfo[x].ContainsKeyValue(manager, "natural", naturalTypes[k]));
                        if (containsThisType) gridPoints[root][i, j].naturalTypes.Add(k);
                    }
                }
            }
            string filePath = Path.Combine(OSMPaths.GetRenderRoot(), $"Coastline{root.sectorFace.GetFaceAcronym()}.txt");
            using (var writer = File.Open(filePath, FileMode.Create))
            {
                using (var bw = new BinaryWriter(writer))
                {
                    bw.Write(badRelations.Count);
                    foreach (var relation in badRelations) bw.Write(relation);
                    for (int i = 0; i < 257; i++)
                    {
                        for (int j = 0; j < 257; j++)
                        {
                            GridPointInfo gridPoint = gridPoints[root][i, j];
                            bw.Write(gridPoint.naturalTypes.Count);
                            foreach (var naturalType in gridPoint.naturalTypes) bw.Write(naturalType);
                            bw.Write(gridPoint.relations.Count);
                            foreach (var relation in gridPoint.relations) bw.Write(relation);
                            bw.Write(gridPoint.ways.Count);
                            foreach (var way in gridPoint.ways) bw.Write(way);
                        }
                    }
                }
            }
        }

        public static OSMMetaFinal GLOBAL_FINAL = null;

        internal static GridPointInfo GetGridPointInfo(ISector sector)
        {
            if (GLOBAL_FINAL == null) LoadAll();
            return GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X, sector.Y];
        }

        internal static bool IsPixelLand(ISector sector)
        {
            if (GLOBAL_FINAL == null) LoadAll();
            int land1 = GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X, sector.Y].naturalTypes.Contains(0) ? 0 : -1;
            int land2 = GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X + 1, sector.Y].naturalTypes.Contains(0) ? 0 : -1;
            int land3 = GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X, sector.Y + 1].naturalTypes.Contains(0) ? 0 : -1;
            int land4 = GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X + 1, sector.Y + 1].naturalTypes.Contains(0) ? 0 : -1;
            if (GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X, sector.Y].naturalTypes.Contains(1)) land1 = 1;
            if (GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X + 1, sector.Y].naturalTypes.Contains(1)) land1 = 1;
            if (GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X, sector.Y + 1].naturalTypes.Contains(1)) land1 = 1;
            if (GLOBAL_FINAL.gridPoints[sector.GetRoot()][sector.X + 1, sector.Y + 1].naturalTypes.Contains(1)) land1 = 1;
            return land1 == 0 && land2 == 0 && land3 == 0 && land4 == 0;
        }

        private static void LoadAll()
        {
            GLOBAL_FINAL = new OSMMetaFinal();
            // init
            GLOBAL_FINAL.gridPoints = new Dictionary<ISector, GridPointInfo[,]>();
            foreach (var root in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                GridPointInfo[,] gp = new GridPointInfo[257, 257];
                for (int x = 0; x < 257; x++)
                {
                    for (int y = 0; y < 257; y++)
                    {
                        gp[x, y] = new GridPointInfo();
                    }
                }
                GLOBAL_FINAL.gridPoints[root] = gp;
            }
            for (int r = 0; r < 6; r++)
            {
                var frRoot = new CubeSector((CubeSector.CubeSectorFace)r, 0, 0, 0);
                string filePath = Path.Combine(OSMPaths.GetRenderRoot(), $"Coastline{frRoot.sectorFace.GetFaceAcronym()}.txt");
                using (var writer = File.Open(filePath, FileMode.Open))
                {
                    using (var br = new BinaryReader(writer))
                    {
                        int badRelationsCount = br.ReadInt32();
                        for (int i = 0; i < badRelationsCount; i++) GLOBAL_FINAL.badRelations.Add(br.ReadInt64());
                        for (int i = 0; i < 257; i++)
                        {
                            for (int j = 0; j < 257; j++)
                            {
                                GridPointInfo gridPoint = GLOBAL_FINAL.gridPoints[frRoot][i, j];
                                int naturalTypesCount = br.ReadInt32();
                                for (int k = 0; k < naturalTypesCount; k++) gridPoint.naturalTypes.Add(br.ReadInt32());
                                int relationsCount = br.ReadInt32();
                                for (int k = 0; k < relationsCount; k++) gridPoint.relations.Add(br.ReadInt64());
                                int waysCount = br.ReadInt32();
                                for (int k = 0; k < waysCount; k++) gridPoint.ways.Add(br.ReadInt64());
                            }
                        }
                    }
                }
            }
        }

        private void XORWithEdge(OSMMetaManager manager, GridPointInfo gridPointInfo, EdgeInfo edge)
        {
            var way = manager.wayInfo[edge.wayID];
            if (manager.wayInfo[edge.wayID].ContainsKeyValue(manager, "natural", "coastline"))
            {
                if (edge.node1 == way.endNode && edge.node2 == way.startNode)
                {
                    // for coast, let's reject all simple shape closures (OLD BUG: don't reject straight-line ways)
                }
                else
                {
                    if (gridPointInfo.naturalTypes.Contains(0))
                    {
                        gridPointInfo.naturalTypes.Remove(0);
                    }
                    else
                    {
                        gridPointInfo.naturalTypes.Add(0);
                    }
                }
            }
            bool relationHasWater = false;
            bool relationHasGlacier = false;
            foreach (var relation in way.relations)
            {
                if (IsWater(manager, manager.relationInfo[relation]))
                {
                    relationHasWater = true;
                }
                if (manager.relationInfo[relation].ContainsKeyValue(manager, "natural", "glacier"))
                {
                    relationHasGlacier = true;
                }
                if (edge.node1 == way.endNode && edge.node2 == way.startNode)
                {
                    // for relations, let's reject all simple shape closures (OLD BUG: don't reject straight-line ways)
                }
                else if (IsWater(manager, manager.relationInfo[relation]) || manager.relationInfo[relation].ContainsKeyValue(manager, "natural", "glacier"))
                {
                    if (badRelations.Contains(relation))
                    {
                        continue;
                    }
                    if (gridPointInfo.relations.Contains(relation))
                    {
                        gridPointInfo.relations.Remove(relation);
                    }
                    else
                    {
                        gridPointInfo.relations.Add(relation);
                    }

                }
            }
            if (!relationHasWater && IsWater(manager, manager.wayInfo[edge.wayID]))
            {
                if (gridPointInfo.ways.Contains(edge.wayID))
                {
                    gridPointInfo.ways.Remove(edge.wayID);
                }
                else
                {
                    gridPointInfo.ways.Add(edge.wayID);
                }
            }
            if (!relationHasGlacier && manager.wayInfo[edge.wayID].ContainsKeyValue(manager, "natural", "glacier"))
            {
                if (gridPointInfo.ways.Contains(edge.wayID))
                {
                    gridPointInfo.ways.Remove(edge.wayID);
                }
                else
                {
                    gridPointInfo.ways.Add(edge.wayID);
                }
            }
        }

        private bool IsWater(OSMMetaManager manager, RelationInfo relationInfo)
        {
            if (relationInfo.ContainsKeyValue(manager, "waterway", "river")) return false; // not area
            if (relationInfo.ContainsKeyValue(manager, "type", "waterway")) return false; // not area
            return relationInfo.ContainsKeyValue(manager, "natural", "water");
        }

        private bool IsWater(OSMMetaManager manager, WayInfo wayInfo)
        {
            if (wayInfo.ContainsKeyValue(manager, "waterway", "river")) return false; // not area
            if (wayInfo.ContainsKeyValue(manager, "type", "waterway")) return false; // not area
            return wayInfo.ContainsKeyValue(manager, "natural", "water");
        }

        // allows us to discard bad relations (bad so far as we can tell, still can't tell if it crosses multiple faces yet)
        private bool IsValidRelation(OSMMetaManager manager, long relation)
        {
            var relationInfo = manager.relationInfo[relation];
            foreach (var way in relationInfo.memids)
            {
                if (!manager.wayInfo.ContainsKey(way)) return true; // benefit of the doubt, for now
            }
            Dictionary<long, int> endCounts = new Dictionary<long, int>();
            foreach (var way in relationInfo.memids)
            {
                if (!endCounts.ContainsKey(manager.wayInfo[way].startNode)) endCounts[manager.wayInfo[way].startNode] = 0;
                endCounts[manager.wayInfo[way].startNode]++;
                if (!endCounts.ContainsKey(manager.wayInfo[way].endNode)) endCounts[manager.wayInfo[way].endNode] = 0;
                endCounts[manager.wayInfo[way].endNode]++;
            }
            foreach (var pair in endCounts)
            {
                if (pair.Value % 2 == 1) return false;
            }
            return true;
        }

        // as a point, represents the land types and relations that contain it
        // as an edge, represents the same states that will be added or removed upon crossing
        public class GridPointInfo
        {
            public HashSet<long> relations = new HashSet<long>();
            public HashSet<long> ways = new HashSet<long>();
            public HashSet<int> naturalTypes = new HashSet<int>(); // by index
        }
    }
}

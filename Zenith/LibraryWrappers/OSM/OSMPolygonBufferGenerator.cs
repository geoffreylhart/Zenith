using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibTessDotNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OsmSharp;
using OsmSharp.Streams;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers
{
    class OSMPolygonBufferGenerator
    {
        // manually implement triangulation algorithm
        // library takes like 8 seconds
        public static List<VertexPositionColor> Tesselate(List<List<ContourVertex>> contours, Microsoft.Xna.Framework.Color color)
        {
            // sometimes this seems to get stuck in an infinite loop?
            var task = Task.Run(() =>
            {
                try
                {
                    Polygon polygon = new Polygon();
                    foreach (var contour in contours)
                    {
                        if (contour.Count > 2)
                        {
                            bool isHole = contour[0].Data != null && ((bool)contour[0].Data);
                            List<Vertex> blah = new List<Vertex>();
                            foreach (var v in contour)
                            {
                                blah.Add(new Vertex(v.Position.X, v.Position.Y));
                            }
                            polygon.AddContour(blah, 0, isHole);
                        }
                    }
                    List<VertexPositionColor> triangles = new List<VertexPositionColor>();
                    if (contours.Count == 0) return triangles;
                    float z = 0;
                    var mesh = polygon.Triangulate();
                    foreach (var triangle in mesh.Triangles)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            var pos = triangle.GetVertex(j);
                            var pos2 = triangle.GetVertex((j + 1) % 3);
                            // TODO: why 1-y?
                            triangles.Add(new VertexPositionColor(new Vector3((float)pos.X, (float)pos.Y, z), color));
                            //triangles.Add(new VertexPositionColor(new Vector3((float)pos2.X, (float)pos2.Y, z), Color.Green));
                        }
                    }
                    return triangles;
                }
                catch (Exception ex)
                {
                    return null;
                }
            });
            if (task.Wait(TimeSpan.FromMinutes(2)))
            {
                if (task.Result == null) throw new NotImplementedException();
                return task.Result;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // breakup that whole osm planet
        static int CURRENT_BREAKUP_STEP = 0; // out of 131,071 steps
        static int READ_BREAKUP_STEP = 0; // easy way to allow continuation, should usually equal (#filesThatArenttoplevel)/3
        public static void SegmentOSMPlanet()
        {
            READ_BREAKUP_STEP = int.Parse(File.ReadAllText(OSMPaths.GetPlanetStepPath()).Split(',')[0]); // file should contain the number of physical breakups that were finished
            List<ISector> quadrants = ZCoords.GetSectorManager().GetTopmostOSMSectors();
            if (READ_BREAKUP_STEP <= CURRENT_BREAKUP_STEP)
            {
                foreach (var quadrant in quadrants)
                {
                    String quadrantPath = OSMPaths.GetSectorPath(quadrant);
                    if (File.Exists(quadrantPath)) File.Delete(quadrantPath); // we're assuming it's corrupted
                    if (!Directory.Exists(Path.GetDirectoryName(quadrantPath))) Directory.CreateDirectory(Path.GetDirectoryName(quadrantPath));
                    var fInfo = new FileInfo(OSMPaths.GetPlanetPath());
                    using (var fileInfoStream = fInfo.OpenRead())
                    {
                        using (var source = new PBFOsmStreamSource(fileInfoStream))
                        {
                            var filtered = source.FilterNodes(x => x.Longitude.HasValue && x.Latitude.HasValue && quadrant.ContainsLongLat(new LongLat(x.Longitude.Value * Math.PI / 180, x.Latitude.Value * Math.PI / 180)), true);
                            using (var stream = new FileInfo(quadrantPath).Open(FileMode.Create, FileAccess.ReadWrite))
                            {
                                var target = new PBFOsmStreamTarget(stream, true);
                                target.RegisterSource(filtered);
                                target.Pull();
                                target.Close();
                            }
                        }
                    }
                }
            }
            BreakupStepDone();
            foreach (var quadrant in quadrants)
            {
                String quadrantPath = OSMPaths.GetSectorPath(quadrant);
                BreakupFile(quadrantPath, quadrant, ZCoords.GetSectorManager().GetHighestOSMZoom());
            }
        }

        private static void BreakupStepDone()
        {
            CURRENT_BREAKUP_STEP++;
            if (READ_BREAKUP_STEP <= CURRENT_BREAKUP_STEP)
            {
                File.WriteAllText(OSMPaths.GetPlanetStepPath(), CURRENT_BREAKUP_STEP + ", " + (CURRENT_BREAKUP_STEP / 131071.0 * 100) + "%");
            }
        }

        // est: since going from 6 to 10 took 1 minute, we might expect doing all 256 would take 256 minutes
        // if we break it up into quadrants using the same library, maybe it'll only take (4+1+1/16...) roughly 5.33 minutes?
        // actually took 8.673 mins (went from 450MB to 455MB)
        // estimated time to segment the whole 43.1 GB planet? 12/28/2018 = 8.673 * 43.1 / 8.05 * 47.7833 = 36.98 hours
        public static void BreakupFile(string filePath, ISector sector, int targetZoom)
        {
            if (sector.Zoom == targetZoom) return;
            List<ISector> quadrants = sector.GetChildrenAtLevel(sector.Zoom + 1);
            if (READ_BREAKUP_STEP <= CURRENT_BREAKUP_STEP)
            {
                foreach (var quadrant in quadrants)
                {
                    String quadrantPath = OSMPaths.GetSectorPath(quadrant);
                    if (File.Exists(quadrantPath)) File.Delete(quadrantPath); // we're assuming it's corrupted
                    if (!Directory.Exists(Path.GetDirectoryName(quadrantPath))) Directory.CreateDirectory(Path.GetDirectoryName(quadrantPath));
                    var fInfo = new FileInfo(filePath);
                    using (var fileInfoStream = fInfo.OpenRead())
                    {
                        using (var source = new PBFOsmStreamSource(fileInfoStream))
                        {
                            var filtered = source.FilterNodes(x => x.Longitude.HasValue && x.Latitude.HasValue && quadrant.ContainsLongLat(new LongLat(x.Longitude.Value * Math.PI / 180, x.Latitude.Value * Math.PI / 180)), true);
                            using (var stream = new FileInfo(quadrantPath).Open(FileMode.Create, FileAccess.ReadWrite))
                            {
                                var target = new PBFOsmStreamTarget(stream, true);
                                target.RegisterSource(filtered);
                                target.Pull();
                                target.Close();
                            }
                        }
                    }
                }
                if (Path.GetFileName(filePath).ToLower() != Path.GetFileName(OSMPaths.GetPlanetPath()).ToLower()) File.Delete(filePath);
            }
            BreakupStepDone();
            foreach (var quadrant in quadrants)
            {
                String quadrantPath = OSMPaths.GetSectorPath(quadrant);
                BreakupFile(quadrantPath, quadrant, targetZoom);
            }
        }

        private static void Compress(string path, string pathGz)
        {
            using (FileStream originalFileStream = new FileInfo(path).OpenRead())
            {
                using (FileStream compressedFileStream = File.Create(pathGz))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }
                }
            }
        }

        // make a lo-rez map showing where there's coast so we can flood-fill it later with land/water
        public static void SaveCoastLineMap(GraphicsDevice graphicsDevice)
        {
            var manager = ZCoords.GetSectorManager();
            int imageSize = 1 << manager.GetHighestOSMZoom();
            foreach (var sector in manager.GetTopmostOSMSectors())
            {
                RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, imageSize, imageSize, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
                graphicsDevice.SetRenderTarget(newTarget);
                List<ISector> sectorsToCheck = new List<ISector>();
                sectorsToCheck.AddRange(sector.GetChildrenAtLevel(ZCoords.GetSectorManager().GetHighestOSMZoom()));
                foreach (var s in sectorsToCheck)
                {
                    if (File.Exists(OSMPaths.GetSectorPath(s)))
                    {
                        GraphicsBasic.DrawScreenRect(graphicsDevice, s.X, s.Y, 1, 1, ContainsCoast(s) ? Microsoft.Xna.Framework.Color.Gray : Microsoft.Xna.Framework.Color.White);
                    }
                    else
                    {
                        GraphicsBasic.DrawScreenRect(graphicsDevice, s.X, s.Y, 1, 1, Microsoft.Xna.Framework.Color.Red);
                    }
                }
                string mapFile = OSMPaths.GetCoastlineImagePath(sector);
                using (var writer = File.OpenWrite(mapFile))
                {
                    newTarget.SaveAsPng(writer, imageSize, imageSize);
                }
            }
        }

        private static bool ContainsCoast(ISector s)
        {
            var source = new PBFOsmStreamSource(new FileInfo(OSMPaths.GetSectorPath(s)).OpenRead());
            foreach (var element in source)
            {
                if (element.Tags.Contains("natural", "coastline")) return true;
            }
            return false;
        }
    }

    internal class NodeComparer : IComparer<OsmSharp.Node>
    {
        public int Compare(OsmSharp.Node x, OsmSharp.Node y)
        {
            return x.Id.Value.CompareTo(y.Id.Value);
        }
    }
}

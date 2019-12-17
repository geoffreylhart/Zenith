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
    class OSMBufferGenerator
    {
        public static List<VertexPositionColor> GetCoastVertices(LineGraph graph, ISector sector)
        {
            if (graph.nodes.Count == 0)
            {
                if (PixelIsLand(sector))
                {
                    List<VertexPositionColor> vertices = new List<VertexPositionColor>();
                    Vector2d topLeft = new Vector2d(0, 0);
                    Vector2d topRight = new Vector2d(1, 0);
                    Vector2d bottomLeft = new Vector2d(0, 1);
                    Vector2d bottomRight = new Vector2d(1, 1);
                    vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, 0), Pallete.GRASS_GREEN));
                    vertices.Add(new VertexPositionColor(new Vector3((float)topRight.X, (float)topRight.Y, 0), Pallete.GRASS_GREEN));
                    vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, 0), Pallete.GRASS_GREEN));
                    vertices.Add(new VertexPositionColor(new Vector3((float)topLeft.X, (float)topLeft.Y, 0), Pallete.GRASS_GREEN));
                    vertices.Add(new VertexPositionColor(new Vector3((float)bottomRight.X, (float)bottomRight.Y, 0), Pallete.GRASS_GREEN));
                    vertices.Add(new VertexPositionColor(new Vector3((float)bottomLeft.X, (float)bottomLeft.Y, 0), Pallete.GRASS_GREEN));
                    return vertices;
                }
            }
            List<List<ContourVertex>> contours = graph.ToContours();
            var outline = TrimLines(sector, contours);
            outline = CloseLines(sector, outline);
            return Tesselate(outline, Pallete.GRASS_GREEN);
        }

#if Windows
        static Dictionary<ISector, Bitmap> landImages = new Dictionary<ISector, Bitmap>();
        private static bool PixelIsLand(ISector sector)
        {
            if (!landImages.ContainsKey(sector.GetRoot())) // 
            {
                string mapFile = OSMPaths.GetCoastlineImagePath(sector);
                landImages[sector.GetRoot()] = new Bitmap(mapFile);
            }
            return landImages[sector.GetRoot()].GetPixel(sector.X, sector.Y) == System.Drawing.Color.FromArgb(255, 0, 255, 0);
        }
#else
        private static bool PixelIsLand(ISector sector)
        {
            return false;
        }
#endif

        // cut off the lines hanging outside of the sector
        // we call this before closing lines to prevent any possible confusion on how lines should connect
        // TODO: this currently breaks aparts closed loops
        private static List<List<ContourVertex>> TrimLines(ISector sector, List<List<ContourVertex>> contours)
        {
            List<List<ContourVertex>> answer = new List<List<ContourVertex>>();
            foreach (var contour in contours)
            {
                bool isLoop = contour.First().Equals(contour.Last());
                List<ContourVertex> currLine = new List<ContourVertex>();
                var firstLine = currLine;
                var lastLine = currLine;
                if (sector.ContainsCoord(new Vector2d(contour[0].Position.X, contour[0].Position.Y))) currLine.Add(contour[0]);
                for (int i = 1; i < contour.Count; i++) // iterate through lines
                {
                    Vector2d v1 = new Vector2d(contour[i - 1].Position.X, contour[i - 1].Position.Y);
                    Vector2d v2 = new Vector2d(contour[i].Position.X, contour[i].Position.Y);
                    bool isInside1 = sector.ContainsCoord(v1);
                    bool isInside2 = sector.ContainsCoord(v2);
                    Vector2d[] intersections = GetIntersections(sector, v1, v2);
                    if (isInside2 && intersections.Length == 0)
                    {
                        currLine.Add(contour[i]);
                        lastLine = currLine;
                    }
                    if (intersections.Length >= 1)
                    {
                        for (int j = 0; j < intersections.Length; j++)
                        {
                            var intersection = intersections[j];
                            currLine.Add(new ContourVertex() { Position = new Vec3() { X = (float)intersection.X, Y = (float)intersection.Y, Z = 0 } });
                            lastLine = currLine;
                            if (((isInside1 ? 0 : 1) + j) % 2 == 0)
                            {
                                answer.Add(currLine);
                                currLine = new List<ContourVertex>();
                            }
                        }
                        if (isInside2)
                        {
                            currLine.Add(contour[i]);
                            lastLine = currLine;
                        }
                    }
                }
                if (isLoop && firstLine != lastLine && firstLine.Count > 0 && lastLine.Count > 0)
                {
                    answer.Remove(firstLine);
                    for (int i = 1; i < firstLine.Count; i++) // don't add the first duplicate
                    {
                        lastLine.Add(firstLine[i]);
                    }
                }
                if (currLine.Count > 0) answer.Add(currLine);
            }
            return answer;
        }

        // do we treat these as straight lines or arc lines?
        // I guess lets do straight lines
        // let's return them in order of intersection
        private static Vector2d[] GetIntersections(ISector sector, Vector2d start, Vector2d end)
        {
            Vector2d topLeft = new Vector2d(0, 0);
            Vector2d topRight = new Vector2d(1, 0);
            Vector2d bottomLeft = new Vector2d(0, 1);
            Vector2d bottomRight = new Vector2d(1, 1);
            List<Vector2d> answer = new List<Vector2d>();
            answer.AddRange(GetIntersections(start, end, topLeft, topRight));
            answer.AddRange(GetIntersections(start, end, topRight, bottomRight));
            answer.AddRange(GetIntersections(start, end, bottomRight, bottomLeft));
            answer.AddRange(GetIntersections(start, end, bottomLeft, topLeft));
            answer.Sort((x, y) => (Math.Pow(x.X - start.X, 2) * Math.Pow(x.Y - start.Y, 2)).CompareTo(Math.Pow(y.X - start.X, 2) * Math.Pow(y.Y - start.Y, 2)));
            return answer.ToArray();
        }

        private static Vector2d[] GetIntersections(Vector2d A, Vector2d B, Vector2d C, Vector2d D)
        {
            Vector2d CmP = new Vector2d(C.X - A.X, C.Y - A.Y);
            Vector2d r = new Vector2d(B.X - A.X, B.Y - A.Y);
            Vector2d s = new Vector2d(D.X - C.X, D.Y - C.Y);

            double CmPxr = CmP.X * r.Y - CmP.Y * r.X;
            double CmPxs = CmP.X * s.Y - CmP.Y * s.X;
            double rxs = r.X * s.Y - r.Y * s.X;

            double rxsr = 1f / rxs;
            double t = CmPxs * rxsr;
            double u = CmPxr * rxsr;

            if ((t >= 0) && (t <= 1) && (u >= 0) && (u <= 1))
            {
                return new[] { new Vector2d(A.X * (1 - t) + B.X * t, A.Y * (1 - t) + B.Y * t) };
            }
            else
            {
                return new Vector2d[0];
            }
        }

        // currently doesn't expect loops
        private static List<List<ContourVertex>> CloseLines(ISector sector, List<List<ContourVertex>> contours)
        {
            var loops = contours.Where(x => x.First().Equals(x.Last())).ToList();
            contours = contours.Where(x => !x.First().Equals(x.Last())).ToList();
            // TODO: did I accidentally properly do the winding rule thing?
            foreach (var contour in contours) contour.Reverse(); // TODO: get rid of hack
            var indices = Enumerable.Range(0, contours.Count * 2).ToList();
            // sorts them going what direction??
            indices.Sort((x, y) => AngleOf(x, sector, contours).CompareTo(AngleOf(y, sector, contours)));
            int[] lookup = new int[contours.Count * 2];
            for (int i = 0; i < contours.Count * 2; i++) lookup[indices[i]] = i;
            var closed = new List<List<ContourVertex>>();
            bool[] visited = new bool[contours.Count * 2];
            for (int i = 0; i < contours.Count * 2; i++)
            {
                if (!visited[i])
                {
                    List<ContourVertex> newLoop = new List<ContourVertex>();
                    int next = indices[(lookup[i] + 1) % (contours.Count * 2)]; // start of a line on the inside
                    while (true)
                    {
                        if (visited[next]) break;
                        visited[next] = true;
                        if (next / contours.Count == 0)
                        {
                            visited[next + contours.Count] = true;
                            for (int j = 0; j < contours[next % contours.Count].Count; j++)
                            {
                                newLoop.Add(contours[next % contours.Count][j]);
                            }
                            ContourVertex edgeStart = contours[next % contours.Count].Last();
                            next = indices[(lookup[next + contours.Count] + 1) % (contours.Count * 2)];
                            // TODO: don't assume the data works
                            ContourVertex edgeEnd = contours[next % contours.Count].First();
                            AddEdgeConnection(newLoop, sector, edgeStart, edgeEnd);
                        }
                        else
                        {
                            visited[next - contours.Count] = true;
                            for (int j = contours[next % contours.Count].Count - 1; j >= 0; j--)
                            {
                                newLoop.Add(contours[next % contours.Count][j]);
                            }
                            ContourVertex edgeStart = contours[next % contours.Count].First();
                            next = indices[(lookup[next - contours.Count] + 1) % (contours.Count * 2)];
                            // TODO: don't assume the data works
                            ContourVertex edgeEnd = contours[next % contours.Count].Last();
                            AddEdgeConnection(newLoop, sector, edgeStart, edgeEnd);
                        }
                    }
                    closed.Add(newLoop);
                }
            }
            closed.AddRange(loops);
            return closed;
        }

        private static void AddEdgeConnection(List<ContourVertex> loop, ISector sector, ContourVertex edgeStart, ContourVertex edgeEnd)
        {
            List<ContourVertex> vertices = new List<ContourVertex>();
            vertices.Add(edgeStart);
            vertices.Add(edgeEnd);
            vertices.Add(ToVertex(new Vector2d(0, 0)));
            vertices.Add(ToVertex(new Vector2d(1, 0)));
            vertices.Add(ToVertex(new Vector2d(0, 1)));
            vertices.Add(ToVertex(new Vector2d(1, 1)));
            vertices.Sort((x, y) => AngleOf(sector, x).CompareTo(AngleOf(sector, y)));
            int start = vertices.IndexOf(edgeStart);
            int end = vertices.IndexOf(edgeEnd);
            if (end < start) end += vertices.Count;
            for (int i = start + 1; i < end; i++)
            {
                loop.Add(vertices[i % vertices.Count]);
            }
        }

        private static ContourVertex ToVertex(Vector2d v)
        {
            ContourVertex vertex = new ContourVertex();
            vertex.Position = new Vec3() { X = (float)v.X, Y = (float)v.Y, Z = 0 };
            return vertex;
        }

        // very special sort
        private static double AngleOf(int index, ISector sector, List<List<ContourVertex>> contours)
        {
            var line = contours[index % contours.Count];
            ContourVertex vertex = line[index / contours.Count == 0 ? 0 : line.Count - 1];
            return AngleOf(sector, vertex);
        }
        private static double AngleOf(ISector sector, ContourVertex vertex)
        {
            double x = vertex.Position.X - 0.5;
            double y = vertex.Position.Y - 0.5;
            return Math.Atan2(-y, x);
        }

        private static List<VertexPositionColor> DrawLines(GraphicsDevice graphicsDevice, ISector sector, List<List<LibTessDotNet.ContourVertex>> contours)
        {
            List<VertexPositionColor> lines = new List<VertexPositionColor>();
            if (contours.Count == 0) return lines;
            float z = 0;
            for (int j = 0; j < contours.Count; j++)
            {
                List<ContourVertex> contour = contours[j];
                Microsoft.Xna.Framework.Color color = new[] { Microsoft.Xna.Framework.Color.Green, Microsoft.Xna.Framework.Color.Blue, Microsoft.Xna.Framework.Color.Red, Microsoft.Xna.Framework.Color.Yellow, Microsoft.Xna.Framework.Color.Orange, Microsoft.Xna.Framework.Color.Cyan, Microsoft.Xna.Framework.Color.Magenta }[j % 7];
                Microsoft.Xna.Framework.Color fadeTo = Microsoft.Xna.Framework.Color.White;
                for (int i = 1; i < contour.Count; i++)
                {
                    double percent = i / (double)(contour.Count - 1); // fade from color to white
                    double percent2 = (i + 1) / (double)(contour.Count - 1); // fade from color to white
                    Microsoft.Xna.Framework.Color newColor = new Microsoft.Xna.Framework.Color(DoThing(color.R, fadeTo.R, percent), DoThing(color.G, fadeTo.G, percent), DoThing(color.B, fadeTo.B, percent));
                    Microsoft.Xna.Framework.Color newColor2 = new Microsoft.Xna.Framework.Color(DoThing(color.R, fadeTo.R, percent2), DoThing(color.G, fadeTo.G, percent2), DoThing(color.B, fadeTo.B, percent2));
                    Vector2d pos1 = new Vector2d(contour[i - 1].Position.X, contour[i - 1].Position.Y);
                    Vector2d pos2 = new Vector2d(contour[i].Position.X, contour[i].Position.Y);
                    Vector2d off1, off2;
                    if (true)
                    {
                        off1 = (pos2 - pos1).RotateCW90().Normalized() * -0.000001;
                    }
                    else
                    {
                        Vector2d pos0 = new Vector2d(contour[i - 2].Position.X, contour[i - 2].Position.Y);
                        off1 = (pos2 - pos0).RotateCW90().Normalized() * -0.000001;
                    }
                    if (true)
                    {
                        off2 = (pos2 - pos1).RotateCW90().Normalized() * -0.000001;
                    }
                    else
                    {
                        Vector2d pos3 = new Vector2d(contour[i + 1].Position.X, contour[i + 1].Position.Y);
                        off2 = (pos3 - pos1).RotateCW90().Normalized() * -0.000001;
                    }
                    lines.Add(new VertexPositionColor(new Vector3(pos2 - off2, z), newColor2));
                    lines.Add(new VertexPositionColor(new Vector3(pos2 + off2, z), newColor2));
                    lines.Add(new VertexPositionColor(new Vector3(pos1 + off1, z), newColor));
                    lines.Add(new VertexPositionColor(new Vector3(pos2 - off2, z), newColor2));
                    lines.Add(new VertexPositionColor(new Vector3(pos1 + off1, z), newColor));
                    lines.Add(new VertexPositionColor(new Vector3(pos1 - off1, z), newColor));
                }
            }
            return lines;
        }

        private static float DoThing(byte r1, byte r2, double percent)
        {
            return (float)((r1 * percent + r2 * (1 - percent)) / 255.0);
        }

        private static void DrawDebugLines(GraphicsDevice graphicsDevice, MercatorSector sector, List<List<LibTessDotNet.ContourVertex>> contours)
        {

        }

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

    internal class NodeComparer : IComparer<Node>
    {
        public int Compare(Node x, Node y)
        {
            return x.Id.Value.CompareTo(y.Id.Value);
        }
    }
}

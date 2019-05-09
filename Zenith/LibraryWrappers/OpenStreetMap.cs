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
using Zenith.ZGraphics;
using Zenith.ZMath;
using static Zenith.EditorGameComponents.FlatComponents.SectorLoader;

namespace Zenith.LibraryWrappers
{
    class OpenStreetMap
    {
        internal static BasicVertexBuffer GetRoads(GraphicsDevice graphicsDevice, Sector sector)
        {
            double widthInFeet = 10.7 * 4; // extra thick
            double circumEarth = 24901 * 5280;
            double width = widthInFeet / circumEarth * 2 * Math.PI;
            return OSM.OSM.GetRoadsFast(OSMPaths.GetSectorPath(sector)).ConstructAsRoads(graphicsDevice, width, Microsoft.Xna.Framework.Color.White);
        }

        internal static BasicVertexBuffer GetCoast(GraphicsDevice graphicsDevice, Sector sector)
        {
            List<Node> nodes = new List<Node>();
            List<Way> ways = new List<Way>();
            using (var reader = new FileInfo(OSMPaths.GetSectorPath(sector)).OpenRead())
            {
                using (var src = new PBFOsmStreamSource(reader))
                {
                    foreach (var element in src)
                    {
                        if (element is Node) nodes.Add((Node)element);
                        if (element.Tags.Contains("natural", "coastline")) ways.Add((Way)element);
                    }
                    if (ways.Count == 0)
                    {
                        if (PixelIsLand(sector))
                        {
                            graphicsDevice.Clear(Pallete.GRASS_GREEN);
                        }
                    }
                }
            }
            // TODO: currently assuming all the data is good
            // TODO: currently ignoring closed loops (lakes)
            Dictionary<long, Way> startsWith = new Dictionary<long, Way>();
            Dictionary<long, Way> endsWith = new Dictionary<long, Way>();
            foreach (var way in ways)
            {
                startsWith[way.Nodes[0]] = way;
                endsWith[way.Nodes[way.Nodes.Length - 1]] = way;
            }
            List<List<LibTessDotNet.ContourVertex>> contours = new List<List<LibTessDotNet.ContourVertex>>();
            foreach (var way in ways)
            {
                if (endsWith.ContainsKey(way.Nodes[0])) continue; // I guess I'm discarding loops here?
                List<LibTessDotNet.ContourVertex> contour = new List<LibTessDotNet.ContourVertex>();
                Way next = way;
                bool first = true;
                while (true)
                {
                    for (int i = first ? 0 : 1; i < next.Nodes.Length; i++)
                    {
                        Node node1 = new Node();
                        node1.Id = next.Nodes[i];
                        int found1 = nodes.BinarySearch(node1, new NodeComparer());
                        if (found1 < 0) continue;
                        node1 = nodes[found1];
                        LongLat longlat1 = new LongLat(node1.Longitude.Value * Math.PI / 180, node1.Latitude.Value * Math.PI / 180);
                        LibTessDotNet.ContourVertex vertex = new LibTessDotNet.ContourVertex();
                        vertex.Position = new LibTessDotNet.Vec3 { X = (float)longlat1.X, Y = (float)longlat1.Y, Z = 0 };
                        contour.Add(vertex);
                    }
                    if (!startsWith.ContainsKey(next.Nodes.Last())) break;
                    next = startsWith[next.Nodes.Last()];
                    first = false;
                }
                if (contour.Count > 0)
                {
                    // super hacky
                    contours.Add(contour);
                }
            }
            var outline = TrimLines(sector, contours);
            outline = CloseLines(sector, outline);
            return new BasicVertexBuffer(graphicsDevice, Tesselate(graphicsDevice, sector, outline), PrimitiveType.TriangleList);
        }

        static Bitmap landImage = null;
        private static bool PixelIsLand(Sector sector)
        {
            if (landImage == null)
            {
                string mapFile = @"..\..\..\..\LocalCache\OpenStreetMaps\Renders\Coastline.PNG";
                landImage = new Bitmap(mapFile);
            }
            return landImage.GetPixel(sector.x, sector.y) == System.Drawing.Color.FromArgb(255, 0, 255, 0);
        }

        // cut off the lines hanging outside of the sector
        // we call this before closing lines to prevent any possible confusion on how lines should connect
        private static List<List<ContourVertex>> TrimLines(Sector sector, List<List<ContourVertex>> contours)
        {
            List<List<ContourVertex>> answer = new List<List<ContourVertex>>();
            foreach (var contour in contours)
            {
                List<ContourVertex> currLine = new List<ContourVertex>();
                bool lastPointInside = sector.ContainsLongLat(new LongLat(contour[0].Position.X, contour[0].Position.Y));
                if (lastPointInside) currLine.Add(contour[0]);
                for (int i = 1; i < contour.Count; i++) // iterate through lines
                {
                    LongLat ll1 = new LongLat(contour[i - 1].Position.X, contour[i - 1].Position.Y);
                    LongLat ll2 = new LongLat(contour[i].Position.X, contour[i].Position.Y);
                    bool isInside = sector.ContainsLongLat(ll2);
                    LongLat[] intersections = sector.GetIntersections(ll1, ll2);
                    if (lastPointInside && intersections.Length == 0)
                    {
                        currLine.Add(contour[i]);
                    }
                    if (intersections.Length >= 1)
                    {
                        foreach (var intersection in intersections)
                        {
                            currLine.Add(new ContourVertex() { Position = new Vec3() { X = (float)intersections[0].X, Y = (float)intersections[0].Y, Z = 0 } });
                            if (lastPointInside)
                            {
                                answer.Add(currLine);
                                currLine = new List<ContourVertex>();
                            }
                            else
                            {
                                currLine.Add(contour[i]);
                            }
                            lastPointInside = !lastPointInside;
                        }
                    }
                }
                if (currLine.Count > 0) answer.Add(currLine);
            }
            return answer;
        }

        // currently doesn't expect loops
        private static List<List<ContourVertex>> CloseLines(Sector sector, List<List<ContourVertex>> contours)
        {
            // TODO: did I accidentally properly do the winding rule thing?
            foreach (var contour in contours) contour.Reverse(); // TODO: get rid of hack
            var indices = Enumerable.Range(0, contours.Count * 2).ToList();
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
            return closed;
        }

        private static void AddEdgeConnection(List<ContourVertex> loop, Sector sector, ContourVertex edgeStart, ContourVertex edgeEnd)
        {
            List<ContourVertex> vertices = new List<ContourVertex>();
            vertices.Add(edgeStart);
            vertices.Add(edgeEnd);
            vertices.Add(ToVertex(sector.TopLeftCorner));
            vertices.Add(ToVertex(sector.TopRightCorner));
            vertices.Add(ToVertex(sector.BottomLeftCorner));
            vertices.Add(ToVertex(sector.BottomRightCorner));
            vertices.Sort((x, y) => AngleOf(sector, x).CompareTo(AngleOf(sector, y)));
            int start = vertices.IndexOf(edgeStart);
            int end = vertices.IndexOf(edgeEnd);
            if (end < start) end += vertices.Count;
            for (int i = start + 1; i < end; i++)
            {
                loop.Add(vertices[i % vertices.Count]);
            }
        }

        private static ContourVertex ToVertex(LongLat longlat)
        {
            ContourVertex vertex = new ContourVertex(); ;
            vertex.Position = new Vec3() { X = (float)longlat.X, Y = (float)longlat.Y, Z = 0 };
            return vertex;
        }

        // very special sort
        private static double AngleOf(int index, Sector sector, List<List<ContourVertex>> contours)
        {
            var line = contours[index % contours.Count];
            ContourVertex vertex = line[index / contours.Count == 0 ? 0 : line.Count - 1];
            return AngleOf(sector, vertex);
        }
        private static double AngleOf(Sector sector, ContourVertex vertex)
        {
            double x = vertex.Position.X - sector.Longitude;
            double y = vertex.Position.Y - sector.Latitude;
            return Math.Atan2(y, x);
        }

        private static void DrawLines(GraphicsDevice graphicsDevice, Sector sector, List<List<LibTessDotNet.ContourVertex>> contours)
        {
            if (contours.Count == 0) return;
            List<VertexPositionColor> lines = new List<VertexPositionColor>();
            float z = -10f;
            for (int j = 0; j < contours.Count; j++)
            {
                List<ContourVertex> contour = contours[j];
                Microsoft.Xna.Framework.Color color = new[] { Microsoft.Xna.Framework.Color.Green, Microsoft.Xna.Framework.Color.Blue, Microsoft.Xna.Framework.Color.Red, Microsoft.Xna.Framework.Color.Yellow, Microsoft.Xna.Framework.Color.Orange, Microsoft.Xna.Framework.Color.Cyan, Microsoft.Xna.Framework.Color.Magenta }[j % 7];
                Microsoft.Xna.Framework.Color fadeTo = Microsoft.Xna.Framework.Color.White;
                for (int i = 1; i < contour.Count; i++)
                {
                    double percent = i / (double)(contour.Count - 1); // fade from color to white
                    double percent2 = (i + 1) / (double)(contour.Count - 1); // fade from color to white
                    Microsoft.Xna.Framework.Color newColor = new Microsoft.Xna.Framework.Color((byte)(color.R * percent2 + fadeTo.R * (1 - percent2)), (byte)(color.G * percent2 + fadeTo.G * (1 - percent2)), (byte)(color.B * percent2 + fadeTo.B * (1 - percent2)));
                    Microsoft.Xna.Framework.Color newColor2 = new Microsoft.Xna.Framework.Color((byte)(color.R * percent2 + fadeTo.R * (1 - percent2)), (byte)(color.G * percent2 + fadeTo.G * (1 - percent2)), (byte)(color.B * percent2 + fadeTo.B * (1 - percent2)));
                    lines.Add(new VertexPositionColor(new Vector3(contour[i - 1].Position.X, contour[i - 1].Position.Y, z), newColor));
                    lines.Add(new VertexPositionColor(new Vector3(contour[i].Position.X, contour[i].Position.Y, z), newColor2));
                }
            }
            VertexBuffer landVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, lines.Count, BufferUsage.WriteOnly);
            landVertexBuffer.SetData(lines.ToArray());
            graphicsDevice.SetVertexBuffer(landVertexBuffer);
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)sector.LeftLongitude, (float)sector.RightLongitude, (float)sector.BottomLatitude, (float)sector.TopLatitude, 1, 1000); // TODO: figure out if flip was appropriate
            basicEffect.VertexColorEnabled = true;
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, landVertexBuffer.VertexCount / 2);
            }
        }

        private static void DrawDebugLines(GraphicsDevice graphicsDevice, Sector sector, List<List<LibTessDotNet.ContourVertex>> contours)
        {

        }

        // manually implement triangulation algorithm
        // library takes like 8 seconds
        private static List<VertexPositionColor> Tesselate(GraphicsDevice graphicsDevice, Sector sector, List<List<LibTessDotNet.ContourVertex>> contours)
        {
            List<Polygon> polygons = new List<Polygon>();
            foreach (var contour in contours)
            {
                if (contour.Count > 2)
                {
                    Polygon polygon = new Polygon();
                    List<Vertex> blah = new List<Vertex>();
                    foreach (var v in contour)
                    {
                        blah.Add(new Vertex(v.Position.X, v.Position.Y));
                    }
                    polygon.AddContour(blah);
                    if (polygon.Count > 2) polygons.Add(polygon); // somestimes doesn't like flat triangles, seems like
                }
            }
            List<VertexPositionColor> triangles = new List<VertexPositionColor>();
            if (contours.Count == 0) return triangles;
            float z = -10f;
            foreach (var polygon in polygons)
            {
                var mesh = polygon.Triangulate();
                foreach (var triangle in mesh.Triangles)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var pos = triangle.GetVertex(j);
                        var pos2 = triangle.GetVertex((j + 1) % 3);
                        // TODO: why 1-y?
                        triangles.Add(new VertexPositionColor(new Vector3((float)pos.X, (float)pos.Y, z), Pallete.GRASS_GREEN));
                        //triangles.Add(new VertexPositionColor(new Vector3((float)pos2.X, (float)pos2.Y, z), Color.Green));
                    }
                }
            }
            return triangles;
        }

        // est: since going from 6 to 10 took 1 minute, we might expect doing all 256 would take 256 minutes
        // if we break it up into quadrants using the same library, maybe it'll only take (4+1+1/16...) roughly 5.33 minutes?
        // actually took 8.673 mins (went from 450MB to 455MB)
        // estimated time to segment the whole 43.1 GB planet? 12/28/2018 = 8.673 * 43.1 / 8.05 * 47.7833 = 36.98 hours
        private static void BreakupFile(string filePath, Sector sector, int targetZoom)
        {
            if (sector.zoom == targetZoom) return;
            List<Sector> quadrants = sector.GetChildrenAtLevel(sector.zoom + 1);
            foreach (var quadrant in quadrants)
            {
                // TODO: this isn't actually restartable. It'll start redoing completed dissected files because it thinks it hasn't been done yet (ex: a zoom3 was turned into all zoom10s)
                String quadrantPath = @"..\..\..\..\LocalCache\OpenStreetMaps\" + quadrant.GetAllParents().Where(x => x.zoom == 5).Single().ToString() + "\\" + quadrant.ToString() + ".osm.pbf";
                //String quadrantPathGz = @"..\..\..\..\LocalCache\OpenStreetMaps\" + quadrant.ToString() + ".osm.pbf.gz";
                if (!File.Exists(quadrantPath))
                {
                    var fInfo = new FileInfo(filePath);
                    using (var fileInfoStream = fInfo.OpenRead())
                    {
                        using (var source = new PBFOsmStreamSource(fileInfoStream))
                        {
                            var filtered = source.FilterBox((float)(quadrant.LeftLongitude * 180 / Math.PI), (float)(quadrant.TopLatitude * 180 / Math.PI),
                                (float)(quadrant.RightLongitude * 180 / Math.PI), (float)(quadrant.BottomLatitude * 180 / Math.PI), true); // left, top, right, bottom
                            using (var stream = new FileInfo(quadrantPath).Open(FileMode.Create, FileAccess.ReadWrite))
                            {
                                var target = new PBFOsmStreamTarget(stream, true);
                                target.RegisterSource(filtered);
                                target.Pull();
                            }
                        }
                    }
                }
                // compression on big files: 2.49GB/8.82GB
                //if (!File.Exists(quadrantPathGz) && new FileInfo(quadrantPath).Length > 1024 && quadrant.zoom == targetZoom) // Note: doesn't apply to root (aka the planet osm)
                //{
                //    Compress(quadrantPath, quadrantPathGz);
                //    File.Delete(quadrantPath);
                //}
                //BreakupFile(quadrantPath, quadrant, targetZoom);
            }
            if (sector.zoom > 0) File.Delete(filePath);
            foreach (var quadrant in quadrants)
            {
                String quadrantPath = @"..\..\..\..\LocalCache\OpenStreetMaps\" + quadrant.GetAllParents().Where(x => x.zoom == 5).Single().ToString() + "\\" + quadrant.ToString() + ".osm.pbf";
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
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 1024, 1024, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            List<Sector> sectorsToCheck = new Sector(0, 0, 0).GetChildrenAtLevel(10);
            foreach (var s in sectorsToCheck)
            {
                GraphicsBasic.DrawScreenRect(graphicsDevice, s.x, s.y, 1, 1, ContainsCoast(s) ? Microsoft.Xna.Framework.Color.Gray : Microsoft.Xna.Framework.Color.White);
            }
            string mapFile = @"..\..\..\..\LocalCache\OpenStreetMaps\Renders\Coastline.PNG";
            using (var writer = File.OpenWrite(mapFile))
            {
                newTarget.SaveAsPng(writer, 1024, 1024);
            }
        }

        private static bool ContainsCoast(Sector s)
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

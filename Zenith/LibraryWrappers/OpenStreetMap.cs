using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OsmSharp;
using OsmSharp.Streams;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.ZGraphics;
using Zenith.ZMath;
using static Zenith.EditorGameComponents.FlatComponents.SectorLoader;

namespace Zenith.LibraryWrappers
{
    class OpenStreetMap
    {
        internal static Texture2D GetRoads(GraphicsDevice graphicsDevice, Sector sector)
        {
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            DrawCoast(graphicsDevice, sector);
            //DrawRoads(graphicsDevice, sector);
            return newTarget;
        }

        private static void DrawCoast(GraphicsDevice graphicsDevice, Sector sector)
        {
            Sector parent = sector.GetChildrenAtLevel(sector.zoom + 1)[0].GetAllParents().Where(x => x.zoom == 10).Single();
            string pensa10Path = @"..\..\..\..\LocalCache\OpenStreetMaps\" + parent.ToString() + ".osm.pbf";
            var src = new PBFOsmStreamSource(new FileInfo(pensa10Path).OpenRead());
            List<Node> nodes = new List<Node>();
            List<Way> ways = new List<Way>();
            foreach (var element in src)
            {
                if (element is Node) nodes.Add((Node)element);
                if (element.Tags.Contains("natural", "coastline")) ways.Add((Way)element);
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
                if (endsWith.ContainsKey(way.Nodes[0])) continue;
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
            //TesselateThenDraw(graphicsDevice, sector, contours);
            //DrawDebugLines(graphicsDevice, sector, contours);
            DrawLines(graphicsDevice, sector, contours);
        }

        private static void DrawLines(GraphicsDevice graphicsDevice, Sector sector, List<List<LibTessDotNet.ContourVertex>> contours)
        {
            if (contours.Count == 0) return;
            List<VertexPositionColor> lines = new List<VertexPositionColor>();
            float z = -10f;
            foreach (var contour in contours)
            {
                Color color = Color.Green;
                Color fadeTo = Color.White;
                for (int i = 1; i < contour.Count; i++)
                {
                    double percent = i / (double)(contour.Count - 1); // fade from color to white
                    double percent2 = (i + 1) / (double)(contour.Count - 1); // fade from color to white
                    Color newColor = new Color((byte)(color.R * percent2 + fadeTo.R * (1 - percent2)), (byte)(color.G * percent2 + fadeTo.G * (1 - percent2)), (byte)(color.B * percent2 + fadeTo.B * (1 - percent2)));
                    Color newColor2 = new Color((byte)(color.R * percent2 + fadeTo.R * (1 - percent2)), (byte)(color.G * percent2 + fadeTo.G * (1 - percent2)), (byte)(color.B * percent2 + fadeTo.B * (1 - percent2)));
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

        private static void TesselateThenDraw(GraphicsDevice graphicsDevice, Sector sector, List<List<LibTessDotNet.ContourVertex>> contours)
        {
            if (contours.Count == 0) return;
            List<VertexPositionColor> triangles = new List<VertexPositionColor>();
            float z = -10f;
            var tess = new LibTessDotNet.Tess();
            foreach (var contour in contours)
            {
                tess.AddContour(contour.ToArray(), LibTessDotNet.ContourOrientation.Original);
            }
            tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);
            for (int i = 0; i < tess.ElementCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var pos = tess.Vertices[tess.Elements[i * 3 + j]].Position;
                    // TODO: why 1-y?
                    triangles.Add(new VertexPositionColor(new Vector3(pos.X, pos.Y, z), Color.Green));
                }
            }
            VertexBuffer landVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, triangles.Count, BufferUsage.WriteOnly);
            landVertexBuffer.SetData(triangles.ToArray());
            graphicsDevice.SetVertexBuffer(landVertexBuffer);
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)sector.LeftLongitude, (float)sector.RightLongitude, (float)sector.BottomLatitude, (float)sector.TopLatitude, 1, 1000); // TODO: figure out if flip was appropriate
            basicEffect.VertexColorEnabled = true;
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, landVertexBuffer.VertexCount - 2);
            }
        }

        private static void DrawRoads(GraphicsDevice graphicsDevice, Sector sector)
        {
            Sector parent = sector.GetChildrenAtLevel(sector.zoom + 1)[0].GetAllParents().Where(x => x.zoom == 10).Single();
            string pensa10Path = @"..\..\..\..\LocalCache\OpenStreetMaps\" + parent.ToString() + ".osm.pbf";
            var src = new PBFOsmStreamSource(new FileInfo(pensa10Path).OpenRead());
            List<Node> nodes = new List<Node>();
            List<Way> highways = new List<Way>();
            foreach (var element in src)
            {
                if (element is Node) nodes.Add((Node)element);
                if (IsHighway(element)) highways.Add((Way)element);
            }
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)sector.LeftLongitude, (float)sector.RightLongitude, (float)sector.BottomLatitude, (float)sector.TopLatitude, 1, 1000); // TODO: figure out if flip was appropriate
            basicEffect.VertexColorEnabled = true;
            var shapeAsVertices = new List<VertexPositionColor>();
            foreach (var highway in highways)
            {
                for (int i = 0; i < highway.Nodes.Length - 1; i++)
                {
                    Node node1 = new Node();
                    Node node2 = new Node();
                    node1.Id = highway.Nodes[i];
                    node2.Id = highway.Nodes[i + 1];
                    int found1 = nodes.BinarySearch(node1, new NodeComparer());
                    int found2 = nodes.BinarySearch(node2, new NodeComparer());
                    if (found1 < 0) continue;
                    if (found2 < 0) continue;
                    node1 = nodes[found1];
                    node2 = nodes[found2];
                    LongLat longlat1 = new LongLat(node1.Longitude.Value * Math.PI / 180, node1.Latitude.Value * Math.PI / 180);
                    LongLat longlat2 = new LongLat(node2.Longitude.Value * Math.PI / 180, node2.Latitude.Value * Math.PI / 180);
                    shapeAsVertices.Add(new VertexPositionColor(new Vector3(longlat1, -10f), Color.White));
                    shapeAsVertices.Add(new VertexPositionColor(new Vector3(longlat2, -10f), Color.White));
                }
            }
            if (shapeAsVertices.Count > 0)
            {
                VertexBuffer vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, shapeAsVertices.Count, BufferUsage.WriteOnly);
                vertexBuffer.SetData(shapeAsVertices.ToArray());
                graphicsDevice.SetVertexBuffer(vertexBuffer);
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, vertexBuffer.VertexCount / 2);
                }
            }
        }

        // est: since going from 6 to 10 took 1 minute, we might expect doing all 256 would take 256 minutes
        // if we break it up into quadrants using the same library, maybe it'll only take (4+1+1/16...) roughly 5.33 minutes?
        // actually took 8.673 mins (went from 450MB to 455MB)
        // estimated time to segment the whole 43.1 GB planet? 12/28/2018 = 8.673 * 43.1 / 8.05 * 47.7833 = 36.98 hours
        private static void BreakupFile(string filePath, SectorLoader.Sector sector, int targetZoom)
        {
            if (sector.zoom == targetZoom) return;
            List<SectorLoader.Sector> quadrants = sector.GetChildrenAtLevel(sector.zoom + 1);
            foreach (var quadrant in quadrants)
            {
                String quadrantPath = @"..\..\..\..\LocalCache\OpenStreetMaps\" + quadrant.ToString() + ".osm.pbf";
                var fInfo = new FileInfo(filePath);
                using (var source = new PBFOsmStreamSource(fInfo.OpenRead()))
                {
                    var filtered = source.FilterBox((float)(quadrant.LeftLongitude * 180 / Math.PI), (float)(quadrant.TopLatitude * 180 / Math.PI),
                        (float)(quadrant.RightLongitude * 180 / Math.PI), (float)(quadrant.BottomLatitude * 180 / Math.PI)); // left, top, right, bottom
                    using (var stream = new FileInfo(quadrantPath).Open(FileMode.Create, FileAccess.ReadWrite))
                    {
                        var target = new PBFOsmStreamTarget(stream);
                        target.RegisterSource(filtered);
                        target.Pull();
                    }
                }
            }
            //File.Delete(filePath); TODO: cant access
            foreach (var quadrant in quadrants)
            {
                String quadrantPath = @"..\..\..\..\LocalCache\OpenStreetMaps\" + quadrant.ToString() + ".osm.pbf";
                BreakupFile(quadrantPath, quadrant, targetZoom);
            }
        }

        private static bool IsHighway(OsmGeo element)
        {
            if (!(element is OsmSharp.Way)) return false;
            foreach (var tag in element.Tags)
            {
                if (tag.Key.Contains("highway"))
                {
                    return true;
                }
            }
            return false;
        }

        // make a lo-rez map showing where there's coast so we can flood-fill it later with land/water
        public static void SaveCoastLineMap(GraphicsDevice graphicsDevice)
        {
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 1024, 1024, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            List<Sector> sectorsToCheck = new Sector(16, 37, 6).GetChildrenAtLevel(10);
            foreach (var s in sectorsToCheck)
            {
                GraphicsBasic.DrawScreenRect(graphicsDevice, s.x, s.y, 1, 1, ContainsCoast(s) ? Color.Gray : Color.White);
            }
            string mapFile = @"..\..\..\..\LocalCache\OpenStreetMaps\Renders\Coastline.PNG";
            using (var writer = File.OpenWrite(mapFile))
            {
                newTarget.SaveAsPng(writer, 1024, 1024);
            }
        }

        private static bool ContainsCoast(Sector s)
        {
            string pensa10Path = @"..\..\..\..\LocalCache\OpenStreetMaps\" + s.ToString() + ".osm.pbf";
            var source = new PBFOsmStreamSource(new FileInfo(pensa10Path).OpenRead());
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

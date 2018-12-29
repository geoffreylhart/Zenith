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
using Zenith.ZMath;

namespace Zenith.LibraryWrappers
{
    class OpenStreetMap
    {
        internal static Texture2D GetRoads(GraphicsDevice graphicsDevice, SectorLoader.Sector sector)
        {
            SectorLoader.Sector parent = null;
            if (sector.zoom > 10)
            {
                parent = sector.GetAllParents().Where(x => x.zoom == 10).Single();
            }
            else
            {
                parent = sector;
            }
            string pensa10Path = @"..\..\..\..\LocalCache\OpenStreetMaps\" + parent.ToString() + ".osm.pbf";
            var source2 = new PBFOsmStreamSource(new FileInfo(pensa10Path).OpenRead());
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            List<Node> nodes = new List<Node>();
            List<Way> highways = new List<Way>();
            foreach (var element in source2)
            {
                if (element is Node) nodes.Add((Node)element);
                if (IsHighway(element)) highways.Add((Way)element);
                //if (element.Tags.Contains("natural", "coastline")) highways.Add((Way)element);
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
            return newTarget;
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
    }

    internal class NodeComparer : IComparer<Node>
    {
        public int Compare(Node x, Node y)
        {
            return x.Id.Value.CompareTo(y.Id.Value);
        }
    }
}

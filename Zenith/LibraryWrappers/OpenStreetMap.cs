using System;
using System.Collections.Generic;
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
            string pensa6Path = @"..\..\..\..\LocalCache\Pensacola6.osm.pbf";
            string pensa10Path = @"..\..\..\..\LocalCache\Pensacola10.osm.pbf";
            if (!File.Exists(pensa10Path))
            {
                // took forever going from the entire north america to a section surrounding pensacola at zoom 6 (47.7833 minutes)
                // going from zoom 10 to 6 only took 1 minute
                using (var source = new PBFOsmStreamSource(new FileInfo(pensa6Path).OpenRead()))
                {
                    var filtered = source.FilterBox((float)(sector.LeftLongitude * 180 / Math.PI), (float)(sector.TopLatitude * 180 / Math.PI),
                        (float)(sector.RightLongitude * 180 / Math.PI), (float)(sector.BottomLatitude * 180 / Math.PI)); // left, top, right, bottom
                    using (var stream = new FileInfo(pensa10Path).Open(FileMode.Create, FileAccess.ReadWrite))
                    {
                        var target = new PBFOsmStreamTarget(stream);
                        target.RegisterSource(filtered);
                        target.Pull();
                    }
                }
            }
            var source2 = new PBFOsmStreamSource(new FileInfo(pensa10Path).OpenRead());
            //LoadTonsOfMaps(graphicsDevice);
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            List<Node> nodes = new List<Node>();
            List<Way> highways = new List<Way>();
            foreach (var element in source2)
            {
                if (element is Node) nodes.Add((Node)element);
                if (IsHighway(element)) highways.Add((Way)element);
            }
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)sector.LeftLongitude, (float)sector.RightLongitude, (float)sector.TopLatitude, (float)sector.BottomLatitude, 1, 1000);
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
            VertexBuffer vertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionColor.VertexDeclaration, shapeAsVertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(shapeAsVertices.ToArray());
            graphicsDevice.SetVertexBuffer(vertexBuffer);
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, vertexBuffer.VertexCount / 2);
            }
            //graphicsDevice.SetRenderTarget(null);
            return newTarget;
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

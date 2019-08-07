using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    class PointCollection
    {
        List<Vector2d> points = new List<Vector2d>();

        public PointCollection(ISector sector, int num)
        {
            Random random = new Random((sector.X * 31 + sector.Y) * 31 + sector.Zoom);
            for (int i = 0; i < num; i++)
            {
                points.Add(RandomPoint(sector, random));
            }
        }

        private Vector2d RandomPoint(ISector sector, Random random)
        {
            // TODO: make evenly distributed?
            double randX = random.NextDouble();
            double randY = random.NextDouble();
            Vector2d topLeft = new Vector2d(sector.X * sector.ZoomPortion, sector.Y * sector.ZoomPortion);
            Vector2d bottomRight = new Vector2d((sector.X + 1) * sector.ZoomPortion, (sector.Y + 1) * sector.ZoomPortion);
            return new Vector2d(topLeft.X * (1 - randX) + bottomRight.X * randX, topLeft.Y * (1 - randY) + bottomRight.Y * randY);
        }

        internal BasicVertexBuffer Construct(GraphicsDevice graphicsDevice, double width, Texture2D texture, ISector sector)
        {
            Random random = new Random((sector.X * 31 + sector.Y) * 31 + sector.Zoom);

            List<int> indices = new List<int>();
            List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
            foreach (var point in points.OrderBy(x => x.Y))
            {
                Vector2d w = new Vector2d(width / 2, 0);
                Vector2d h = new Vector2d(0, width / 2);
                h *= Math.Cos(point.Y);
                Vector2d topLeft = point - w - h;
                Vector2d topRight = point + w - h;
                Vector2d bottomLeft = point - w + h;
                Vector2d bottomRight = point + w + h;
                int i = vertices.Count;
                vertices.Add(new VertexPositionTexture(new Vector3(topLeft, -10f), new Vector2(0, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(topRight, -10f), new Vector2(1, 0)));
                vertices.Add(new VertexPositionTexture(new Vector3(bottomLeft, -10f), new Vector2(0, 1)));
                vertices.Add(new VertexPositionTexture(new Vector3(bottomRight, -10f), new Vector2(1, 1)));
                indices.Add(i);
                indices.Add(i + 1);
                indices.Add(i + 3);
                indices.Add(i);
                indices.Add(i + 3);
                indices.Add(i + 2);
            }
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, texture, false, PrimitiveType.TriangleList);
        }

        internal PointCollection KeepWithin(List<VertexPositionColor> coastTriangles)
        {
            var rtree = new STRtree<Polygon>();
            for (int i = 0; i < coastTriangles.Count / 3; i++)
            {
                Coordinate[] coords = new Coordinate[] { new Coordinate(coastTriangles[i * 3].Position.X, coastTriangles[i * 3].Position.Y),
                    new Coordinate(coastTriangles[i * 3 + 1].Position.X, coastTriangles[i * 3 + 1].Position.Y),
                    new Coordinate(coastTriangles[i * 3 + 2].Position.X, coastTriangles[i * 3 + 2].Position.Y),
                    new Coordinate(coastTriangles[i * 3].Position.X, coastTriangles[i * 3].Position.Y)
                };
                var polygon = new Polygon(new LinearRing(coords));
                rtree.Insert(polygon.EnvelopeInternal, polygon);
            }
            rtree.Build();
            List<Vector2d> newpoints = new List<Vector2d>();
            foreach (var x in points)
            {
                bool contains = false;
                foreach (var p in rtree.Query(new Envelope(x.X, x.X, x.Y, x.Y)))
                {
                    if (p.Covers(new NetTopologySuite.Geometries.Point(x.X, x.Y)))
                    {
                        contains = true;
                        break;
                    }
                }
                if (contains) newpoints.Add(x);
            }
            points = newpoints;
            return this;
        }

        internal PointCollection ExcludeWithin(List<VertexPositionColor> lakeTriangles)
        {
            var rtree = new STRtree<Polygon>();
            for (int i = 0; i < lakeTriangles.Count / 3; i++)
            {
                Coordinate[] coords = new Coordinate[] { new Coordinate(lakeTriangles[i * 3].Position.X, lakeTriangles[i * 3].Position.Y),
                    new Coordinate(lakeTriangles[i * 3 + 1].Position.X, lakeTriangles[i * 3 + 1].Position.Y),
                    new Coordinate(lakeTriangles[i * 3 + 2].Position.X, lakeTriangles[i * 3 + 2].Position.Y),
                    new Coordinate(lakeTriangles[i * 3].Position.X, lakeTriangles[i * 3].Position.Y)
                };
                var polygon = new Polygon(new LinearRing(coords));
                rtree.Insert(polygon.EnvelopeInternal, polygon);
            }
            rtree.Build();
            List<Vector2d> newpoints = new List<Vector2d>();
            foreach (var x in points)
            {
                bool contains = false;
                foreach (var p in rtree.Query(new Envelope(x.X, x.X, x.Y, x.Y)))
                {
                    if (p.Covers(new NetTopologySuite.Geometries.Point(x.X, x.Y)))
                    {
                        contains = true;
                        break;
                    }
                }
                if (!contains) newpoints.Add(x);
            }
            points = newpoints;
            return this;
        }

        internal PointCollection RemoveNear(LineGraph roads, double minDis)
        {
            if (roads.nodes.Count == 0) return this;
            var rtree = new STRtree<LineString>();
            foreach (var node in roads.nodes)
            {
                foreach (var next in node.nextConnections)
                {
                    Coordinate[] coords = new Coordinate[] { new Coordinate(node.pos.X, node.pos.Y), new Coordinate(next.pos.X, next.pos.Y) };
                    LineString ls = new LineString(coords);
                    rtree.Insert(ls.EnvelopeInternal, ls);
                }
            }
            rtree.Build();
            List<Vector2d> newpoints = new List<Vector2d>();
            foreach (var x in points)
            {
                bool isNear = false;
                foreach (var ls in rtree.Query(new Envelope(x.X - minDis, x.X + minDis, x.Y - minDis, x.Y + minDis)))
                {
                    if (ls.IsWithinDistance(new NetTopologySuite.Geometries.Point(x.X, x.Y), minDis))
                    {
                        isNear = true;
                        break;
                    }
                }
                if (!isNear)
                {
                    newpoints.Add(x);
                }
            }
            points = newpoints;
            return this;
        }
    }
}

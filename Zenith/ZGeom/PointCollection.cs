using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NetTopologySuite.Geometries;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    class PointCollection
    {
        List<Vector2d> points = new List<Vector2d>();

        public PointCollection(Sector sector, int num)
        {
            Random random = new Random((sector.x * 31 + sector.y) * 31 + sector.zoom);
            for (int i = 0; i < num; i++)
            {
                points.Add(RandomPoint(sector, random));
            }
        }

        private Vector2d RandomPoint(Sector sector, Random random)
        {
            double randX = random.NextDouble();
            double randY = random.NextDouble();
            var topLeft = sector.TopLeftCorner;
            var bottomRight = sector.BottomRightCorner;
            return new Vector2d(topLeft.X * (1 - randX) + bottomRight.X * randX, topLeft.Y * (1 - randY) + bottomRight.Y * randY);
        }

        internal BasicVertexBuffer Construct(GraphicsDevice graphicsDevice, double width, Texture2D texture, Sector sector)
        {
            Random random = new Random((sector.x * 31 + sector.y) * 31 + sector.zoom);

            List<int> indices = new List<int>();
            List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
            foreach (var point in points)
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
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, texture, PrimitiveType.TriangleList);
        }

        internal PointCollection KeepWithin(List<VertexPositionColor> coastTriangles)
        {
            IGeometry collection = null;
            // TODO: why didn't our first attempt work??
            //IGeometry[] triangles = new IGeometry[coastTriangles.Count / 3];
            for (int i = 0; i < coastTriangles.Count / 3; i++)
            {
                Coordinate[] coords = new Coordinate[] { new Coordinate(coastTriangles[i * 3].Position.X, coastTriangles[i * 3].Position.Y),
                    new Coordinate(coastTriangles[i * 3 + 1].Position.X, coastTriangles[i * 3 + 1].Position.Y),
                    new Coordinate(coastTriangles[i * 3 + 2].Position.X, coastTriangles[i * 3 + 2].Position.Y),
                    new Coordinate(coastTriangles[i * 3].Position.X, coastTriangles[i * 3].Position.Y)
                };
                //triangles[i] = new Polygon(new LinearRing(coords));
                if (collection == null)
                {
                    collection = new Polygon(new LinearRing(coords));
                }
                else
                {
                    collection = collection.Union(new Polygon(new LinearRing(coords)));
                }
            }
            //IGeometry collection = new GeometryCollection(triangles);
            List<Vector2d> newpoints = new List<Vector2d>();
            foreach (var x in points)
            {
                if (collection.Covers(new NetTopologySuite.Geometries.Point(x.X, x.Y)))
                {
                    newpoints.Add(x);
                }
            }
            points = newpoints;
            return this;
        }

        internal PointCollection RemoveNear(LineGraph roads, double minDis)
        {
            if (roads.nodes.Count == 0) return this;
            List<LineString> lineStrings = new List<LineString>();
            foreach (var node in roads.nodes)
            {
                foreach (var next in node.nextConnections)
                {
                    Coordinate[] coords = new Coordinate[] { new Coordinate(node.pos.X, node.pos.Y), new Coordinate(next.pos.X, next.pos.Y) };
                    lineStrings.Add(new LineString(coords));
                }
            }
            IGeometry collection = new MultiLineString(lineStrings.ToArray());
            List<Vector2d> newpoints = new List<Vector2d>();
            foreach (var x in points)
            {
                if (collection.Distance(new NetTopologySuite.Geometries.Point(x.X, x.Y)) > minDis)
                {
                    newpoints.Add(x);
                }
            }
            points = newpoints;
            return this;
        }
    }
}

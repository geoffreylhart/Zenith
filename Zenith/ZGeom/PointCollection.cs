using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    }
}

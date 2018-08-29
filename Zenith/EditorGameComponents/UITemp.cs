using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Helpers;

namespace Zenith.EditorGameComponents
{
    class UITemp
    {
        static float CORNER_RADIUS = 5;
        static float TAB_HEIGHT = 30;
        static int ARC_REZ = 5;
        internal static void DrawThoseTabs(int x, int y, int w, int h, GraphicsDevice graphicsDevice)
        {
            List<List<Vector2>> shapes = new List<List<Vector2>>();
            var mainShape = new List<Vector2>();
            AddArc(mainShape, x + CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 180, 90);
            AddArc(mainShape, x + 150 - CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS, -90, 0);
            AddArc(mainShape, x + 150 + CORNER_RADIUS, y - TAB_HEIGHT + CORNER_RADIUS, CORNER_RADIUS, 180, 90);
            AddArc(mainShape, x + 250 - CORNER_RADIUS, y - TAB_HEIGHT + CORNER_RADIUS, CORNER_RADIUS, 90, 0);
            AddArc(mainShape, x + 250 + CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS, 180, 270);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 90, 0);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, 0, -90);
            AddArc(mainShape, x + CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, -90, -180);
            DrawShapeWithCenter(mainShape, x + 200, y + h / 2, Color.Black, graphicsDevice);
            DrawLinesWithGradient(mainShape, 2, Color.White, graphicsDevice);
        }

        // TODO: can't do it this way, I'll have to use the spritebatch or something, I'm betting
        private static void DrawLinesWithGradient(List<Vector2> shape, float thickness, Color color, GraphicsDevice graphicsDevice)
        {
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            for (int i = 0; i < shape.Count; i++)
            {
                var prev = shape[(i + shape.Count - 1) % shape.Count];
                var curr = shape[i];
                var next = shape[(i + 1) % shape.Count];
                var next2 = shape[(i + 2) % shape.Count];
                var normal1 = NormalOf(prev, curr);
                var normal2 = NormalOf(curr, next);
                var normal3 = NormalOf(next, next2);
                var normal12 = normal1 + normal2;
                var normal23 = normal2 + normal3;
                normal12 /= normal12.Length();
                normal23 /= normal23.Length();
                var v1 = new Vector3(curr - normal12 * thickness / 2, -10f);
                var v2 = new Vector3(curr + normal12 * thickness / 2, -10f);
                var v3 = new Vector3(next + normal23 * thickness / 2, -10f);
                var v4 = new Vector3(next - normal23 * thickness / 2, -10f);
                vertices.Add(new VertexPositionColor(v1, color));
                vertices.Add(new VertexPositionColor(v2, color));
                vertices.Add(new VertexPositionColor(v4, color));
                vertices.Add(new VertexPositionColor(v4, color));
                vertices.Add(new VertexPositionColor(v2, color));
                vertices.Add(new VertexPositionColor(v3, color));
            }
            float minX = vertices.Min(x => x.Position.X);
            float maxX = vertices.Max(x => x.Position.X);
            for (int i = 0; i < vertices.Count; i++)
            {
                float alpha = (vertices[i].Position.X - minX) / (maxX - minX);
                alpha = 0;
                vertices[i] = new VertexPositionColor(vertices[i].Position, new Color(Color.White, alpha));
            }
            DrawVertices(vertices, graphicsDevice);
        }

        // clockwise points outwards
        private static Vector2 NormalOf(Vector2 v1, Vector2 v2)
        {
            var diff = v2 - v1;
            var ans = new Vector2(diff.Y, -diff.X); // effectively, rotate -90
            return ans / ans.Length();
        }

        private static void DrawShapeWithCenter(List<Vector2> shape, float cx, float cy, Color color, GraphicsDevice graphicsDevice)
        {
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            for (int i = 0; i < shape.Count; i++)
            {
                vertices.Add(new VertexPositionColor(new Vector3(shape[i].X, shape[i].Y, -10f), color));
                vertices.Add(new VertexPositionColor(new Vector3(shape[(i + 1) % shape.Count].X, shape[(i + 1) % shape.Count].Y, -10f), color));
                vertices.Add(new VertexPositionColor(new Vector3(cx, cy, -10f), color));
            }
            DrawVertices(vertices, graphicsDevice);
        }

        private static void DrawVertices(List<VertexPositionColor> vertices, GraphicsDevice graphicsDevice)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 1, 1000);
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, vertices.ToArray());
            }
        }

        private static void AddArc(List<Vector2> shape, float x, float y, float radius, float startDegrees, float endDegrees)
        {
            for (int i = 0; i <= ARC_REZ; i++)
            {
                float ratio = i / (float)ARC_REZ;
                float angle = ratio * endDegrees + (1 - ratio) * startDegrees;
                float ax = x + (float)Math.Cos(angle * Math.PI / 180) * radius;
                float ay = y + -(float)Math.Sin(angle * Math.PI / 180) * radius; // why negative? (because I was thinking with a normal unit circle)
                shape.Add(new Vector2(ax, ay));
            }
        }
    }
}

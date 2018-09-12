using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Helpers;

namespace Zenith.EditorGameComponents
{
    class UITemp
    {
        static RenderTarget2D layer1 = null;
        static RenderTarget2D layer2 = null;
        // gonna make all of these transparent to test
        static RenderTarget2D layer3 = null;
        static RenderTarget2D layer4 = null;
        static RenderTarget2D layer5 = null;
        static Effect blurHoriz = null;
        static Effect blurVert = null;
        static float CORNER_RADIUS = 5;
        static int ARC_REZ = 5;
        internal static void DrawFrontTab(int x, int y, int w, int h, int tabOffset, int tabWidth, int tabHeight, GraphicsDevice graphicsDevice, RenderTarget2D renderSource)
        {
            InitBlurEffect();
            InitLayers(graphicsDevice);
            List<List<Vector2>> shapes = new List<List<Vector2>>();
            var mainShape = new List<Vector2>();
            AddArc(mainShape, x + CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 180, 90); // top left corner
            AddArc(mainShape, x + tabOffset - CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS, -90, 0);
            AddArc(mainShape, x + tabOffset + CORNER_RADIUS, y - tabHeight + CORNER_RADIUS, CORNER_RADIUS, 180, 90);
            AddArc(mainShape, x + tabOffset + tabWidth - CORNER_RADIUS, y - tabHeight + CORNER_RADIUS, CORNER_RADIUS, 90, 0);
            AddArc(mainShape, x + tabOffset + tabWidth + CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS, 180, 270);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 90, 0);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, 0, -90);
            AddArc(mainShape, x + CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, -90, -180);
            float minx = mainShape.Min(v => v.X);
            float maxx = mainShape.Max(v => v.X);
            float miny = mainShape.Min(v => v.Y);
            float maxy = mainShape.Max(v => v.Y);
            DrawBlurredShape(OffsetShape(mainShape, 5), x + tabOffset + tabWidth / 2, y + h / 2, new Color(0, 180, 255, 255), graphicsDevice, null);
            BlurSection((int)minx, (int)miny, (int)maxx, (int)maxy, graphicsDevice, renderSource, layer1, layer2);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.SetRenderTarget(null);
            DrawShapeWithCenter(mainShape, x + tabOffset + tabWidth / 2, y + h / 2, Color.Gray, graphicsDevice, layer2);
            graphicsDevice.SetRenderTarget(null);
            DrawLinesWithGradient(mainShape, 2, Color.White, graphicsDevice);
        }

        private static void DrawBlurredShape(List<Vector2> shape, int cx, int cy, Color color, GraphicsDevice graphicsDevice, RenderTarget2D target)
        {
            float minx = shape.Min(v => v.X);
            float maxx = shape.Max(v => v.X);
            float miny = shape.Min(v => v.Y);
            float maxy = shape.Max(v => v.Y);
            int pad = 20;
            graphicsDevice.SetRenderTarget(layer3);
            graphicsDevice.Clear(Color.Transparent); // TODO: figure out why alpha is acting so weirdly
            DrawShapeWithCenter(shape, cx, cy, color, graphicsDevice, null);
            graphicsDevice.SetRenderTarget(null);
            BlurSection((int)minx - pad, (int)miny - pad, (int)maxx + pad, (int)maxy + pad, graphicsDevice, layer3, layer4, layer5);
            graphicsDevice.SetRenderTarget(null);
            CopySection((int)minx - pad, (int)miny - pad, (int)maxx + pad, (int)maxy + pad, graphicsDevice, layer5);
        }

        internal static void DrawBackTab(int x, int y, int w, int h, Color color, GraphicsDevice graphicsDevice)
        {
            InitBlurEffect();
            InitLayers(graphicsDevice);
            var mainShape = new List<Vector2>();
            AddArc(mainShape, x - CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS, -90, 0);
            AddArc(mainShape, x + CORNER_RADIUS, y - h + CORNER_RADIUS, CORNER_RADIUS, 180, 90);
            AddArc(mainShape, x + w - CORNER_RADIUS, y - h + CORNER_RADIUS, CORNER_RADIUS, 90, 0);
            AddArc(mainShape, x + w + CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS, -180, -90);
            var blurShape = new List<Vector2>(); // specially crafted to not overlap with the other blur shape at all
            AddArc(blurShape, x - CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS - 5, -90, 0);
            AddArc(blurShape, x + CORNER_RADIUS, y - h + CORNER_RADIUS, CORNER_RADIUS + 5, 180, 90);
            AddArc(blurShape, x + w - CORNER_RADIUS, y - h + CORNER_RADIUS, CORNER_RADIUS + 5, 90, 0);
            AddArc(blurShape, x + w + CORNER_RADIUS, y - CORNER_RADIUS, CORNER_RADIUS - 5, -180, -90);
            DrawBlurredShape(blurShape, x + w / 2, y + h / 2, new Color(0, 180, 255, 255), graphicsDevice, null);
            graphicsDevice.SetRenderTarget(null);
            DrawShapeWithCenter(mainShape, x + w / 2, y + h / 2, color, graphicsDevice, null);
            graphicsDevice.SetRenderTarget(null);
            DrawLinesWithGradient(mainShape, 2, Color.White, graphicsDevice);
        }

        internal static void DrawStyledBoxBack(int x, int y, int w, int h, GraphicsDevice graphicsDevice, RenderTarget2D renderSource)
        {
            InitBlurEffect();
            InitLayers(graphicsDevice);
            List<List<Vector2>> shapes = new List<List<Vector2>>();
            var mainShape = new List<Vector2>();
            AddArc(mainShape, x + CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 180, 90);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 90, 0);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, 0, -90);
            AddArc(mainShape, x + CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, -90, -180);
            float minx = mainShape.Min(v => v.X);
            float maxx = mainShape.Max(v => v.X);
            float miny = mainShape.Min(v => v.Y);
            float maxy = mainShape.Max(v => v.Y);
            DrawBlurredShape(OffsetShape(mainShape, 5), x + 100, y + h / 2, new Color(0, 180, 255, 255), graphicsDevice, null);
            BlurSection((int)minx, (int)miny, (int)maxx, (int)maxy, graphicsDevice, renderSource, layer1, layer2);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.SetRenderTarget(null);
            DrawShapeWithCenter(mainShape, x + 100, y + h / 2, Color.Gray, graphicsDevice, layer2);
            graphicsDevice.SetRenderTarget(null);
            DrawLinesWithGradient(mainShape, 2, Color.White, graphicsDevice);
        }

        internal static void DrawStyledBoxFront(int x, int y, int w, int h, GraphicsDevice graphicsDevice, RenderTarget2D renderSource)
        {
            List<List<Vector2>> shapes = new List<List<Vector2>>();
            var mainShape = new List<Vector2>();
            AddArc(mainShape, x + CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 180, 90);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + CORNER_RADIUS, CORNER_RADIUS, 90, 0);
            AddArc(mainShape, x + w - CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, 0, -90);
            AddArc(mainShape, x + CORNER_RADIUS, y + h - CORNER_RADIUS, CORNER_RADIUS, -90, -180);
            DrawLinesWithGradient(mainShape, 2, Color.White, graphicsDevice);
        }

        private static List<Vector2> OffsetShape(List<Vector2> v, float o)
        {
            List<Vector2> newv = new List<Vector2>();
            for (int i = 0; i < v.Count; i++)
            {
                var prev = v[(i + v.Count - 1) % v.Count];
                var curr = v[i];
                var next = v[(i + 1) % v.Count];
                var next2 = v[(i + 2) % v.Count];
                var normal1 = NormalOf(prev, curr);
                var normal2 = NormalOf(curr, next);
                var normal3 = NormalOf(next, next2);
                var normal12 = normal1 + normal2;
                var normal23 = normal2 + normal3;
                normal12 /= normal12.Length();
                normal23 /= normal23.Length();
                var v1 = curr + normal12 * o;
                newv.Add(v1);
            }
            return newv;
        }

        private static void CopySection(int minx, int miny, int maxx, int maxy, GraphicsDevice graphicsDevice, RenderTarget2D src)
        {
            Rectangle rect = new Rectangle(minx, miny, maxx - minx, maxy - miny);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null);
            spriteBatch.Draw(src, rect, rect, Color.White);
            spriteBatch.End();
        }

        private static void DrawShapeWithCenter(List<Vector2> shape, int cx, int cy, Color color, GraphicsDevice graphicsDevice, RenderTarget2D src)
        {
            List<VertexPositionColorTexture> vertices = new List<VertexPositionColorTexture>();
            for (int i = 0; i < shape.Count; i++)
            {
                vertices.Add(new VertexPositionColorTexture(new Vector3(shape[i].X, shape[i].Y, -10f), color, new Vector2(shape[i].X / (float)graphicsDevice.Viewport.Width, shape[i].Y / (float)graphicsDevice.Viewport.Height)));
                vertices.Add(new VertexPositionColorTexture(new Vector3(shape[(i + 1) % shape.Count].X, shape[(i + 1) % shape.Count].Y, -10f), color, new Vector2((shape[(i + 1) % shape.Count].X) / (float)graphicsDevice.Viewport.Width, (shape[(i + 1) % shape.Count].Y) / (float)graphicsDevice.Viewport.Height)));
                vertices.Add(new VertexPositionColorTexture(new Vector3(cx, cy, -10f), color, new Vector2(cx / (float)graphicsDevice.Viewport.Width, cy / (float)graphicsDevice.Viewport.Height)));
            }
            DrawVertices(vertices, graphicsDevice, src);
        }

        private static void DrawVertices(List<VertexPositionColorTexture> vertices, GraphicsDevice graphicsDevice, RenderTarget2D src)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            if (src != null)
            {
                basicEffect.TextureEnabled = true;
                basicEffect.Texture = src;
            }
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 1, 1000);
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives<VertexPositionColorTexture>(PrimitiveType.TriangleList, vertices.ToArray());
            }
        }

        static private float[] kernel;
        static private Vector2[] offsetsHoriz;
        static private Vector2[] offsetsVert;
        static void ComputeKernel(int blurRadius, float blurAmount)
        {
            kernel = new float[15];
            int radius = blurRadius;
            float amount = blurAmount;

            kernel = null;
            kernel = new float[radius * 2 + 1];
            float sigma = radius / amount;

            float twoSigmaSquare = 2.0f * sigma * sigma;
            float sigmaRoot = (float)Math.Sqrt(twoSigmaSquare * Math.PI);
            float total = 0.0f;
            float distance = 0.0f;
            int index = 0;

            for (int i = -radius; i <= radius; ++i)
            {
                distance = i * i;
                index = i + radius;
                kernel[index] = (float)Math.Exp(-distance / twoSigmaSquare) / sigmaRoot;
                total += kernel[index];
            }

            for (int i = 0; i < kernel.Length; ++i)
                kernel[i] /= total;
            // do offsets
            float textureWidth = 640f;
            float textureHeight = 640f;
            offsetsHoriz = new Vector2[radius * 2 + 1];
            offsetsVert = new Vector2[radius * 2 + 1];

            int index2 = 0;
            float xOffset = 1.0f / textureWidth;
            float yOffset = 1.0f / textureHeight;

            for (int i = -radius; i <= radius; ++i)
            {
                index2 = i + radius;
                offsetsHoriz[index2] = new Vector2(i * xOffset, 0.0f);
                offsetsVert[index2] = new Vector2(0.0f, i * yOffset);
            }
        }

        public static void InitBlurEffect()
        {
            if (blurHoriz != null) return;

            ComputeKernel(7, 2);
            blurHoriz = GlobalContent.BlurShader.Clone();
            blurVert = GlobalContent.BlurShader.Clone();
            blurHoriz.Parameters["weights"].SetValue(kernel);
            blurHoriz.Parameters["offsets"].SetValue(offsetsHoriz);
            blurVert.Parameters["weights"].SetValue(kernel);
            blurVert.Parameters["offsets"].SetValue(offsetsVert);
        }

        private static void InitLayers(GraphicsDevice graphicsDevice)
        {
            if (layer1 != null && (layer1.Width != graphicsDevice.Viewport.Width || layer1.Height != graphicsDevice.Viewport.Height))
            {
                layer1.Dispose();
                layer1 = null;
                layer2.Dispose();
                layer2 = null;
                layer3.Dispose();
                layer3 = null;
                layer4.Dispose();
                layer4 = null;
                layer5.Dispose();
                layer5 = null;
            }
            if (layer1 != null) return;
            layer1 = new RenderTarget2D(
                 graphicsDevice,
                 graphicsDevice.Viewport.Width,
                 graphicsDevice.Viewport.Height,
                 false,
                 graphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
            layer2 = new RenderTarget2D(
                 graphicsDevice,
                 graphicsDevice.Viewport.Width,
                 graphicsDevice.Viewport.Height,
                 false,
                 graphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
            layer3 = new RenderTarget2D(
                 graphicsDevice,
                 graphicsDevice.Viewport.Width,
                 graphicsDevice.Viewport.Height,
                 false,
                 graphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
            layer4 = new RenderTarget2D(
                 graphicsDevice,
                 graphicsDevice.Viewport.Width,
                 graphicsDevice.Viewport.Height,
                 false,
                 graphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
            layer5 = new RenderTarget2D(
                 graphicsDevice,
                 graphicsDevice.Viewport.Width,
                 graphicsDevice.Viewport.Height,
                 false,
                 graphicsDevice.PresentationParameters.BackBufferFormat,
                 DepthFormat.Depth24);
            //SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            //foreach(var lay in new[] { layer3, layer4, layer5 })
            //{
            //    graphicsDevice.SetRenderTarget(lay);
            //    graphicsDevice.Clear(Color.White);
            //    graphicsDevice.SetRenderTarget(null);
            //    spriteBatch.Begin();
            //    spriteBatch.Draw(lay, new Vector2(0, 0), Color.White);
            //    spriteBatch.End();
            //}
        }

        // returns the main viewport but blurred within a specific sections
        // uses layer1 and layer2
        private static void BlurSection(int x1, int y1, int x2, int y2, GraphicsDevice graphicsDevice, RenderTarget2D src, RenderTarget2D intermediate, RenderTarget2D dest)
        {
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            float z = -10f;
            List<VertexPositionColor> rect = new List<VertexPositionColor>();
            rect.Add(new VertexPositionColor(new Vector3(x1, y2, z), Color.White)); // bottom-left
            rect.Add(new VertexPositionColor(new Vector3(x1, y1, z), Color.White)); // top-left
            rect.Add(new VertexPositionColor(new Vector3(x2, y2, z), Color.White)); // bottom-right
            rect.Add(new VertexPositionColor(new Vector3(x2, y1, z), Color.White)); // top-right
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 1, 1000);
            var actualEffect = blurHoriz;
            graphicsDevice.SetRenderTarget(intermediate);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
    SamplerState.LinearClamp, DepthStencilState.Default,
    RasterizerState.CullNone, actualEffect);
            var actualRect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            spriteBatch.Draw(src, actualRect, actualRect, Color.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(dest);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
     SamplerState.LinearClamp, DepthStencilState.Default,
     RasterizerState.CullNone, blurVert);
            spriteBatch.Draw(intermediate, actualRect, actualRect, Color.White);
            spriteBatch.End();
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
                float alpha = 1 - (vertices[i].Position.X - minX) / (maxX - minX);
                Color cc2 = Color.White * alpha;
                // what on earth...? why does the alpha seem to be the max(a,r,g,b)?
                // because at some point it switches to additive or something, and a must begin to take on a totally different meaning
                vertices[i] = new VertexPositionColor(vertices[i].Position, cc2);
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

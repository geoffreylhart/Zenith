using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;
using Zenith.MathHelpers;

namespace Zenith.EditorGameComponents
{
    // Controls:
    // - Right click a point to remove it
    // - Left click a point to drag it
    // - Left click on a line to add a point
    public class SphericalGeometryEditor : DrawableGameComponent
    {
        private EditorCamera camera;
        private List<Vector2> shape;
        private Vector2 previewPoint;
        private static int HALF_SIZE = 4;
        private int draggingPointIndex = -1;
        private static bool oldLeft = false;
        private static bool oldRight = false;

        public SphericalGeometryEditor(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
            shape = new List<Vector2>() { new Vector2(0, 0), new Vector2(0, 0.1f), new Vector2(0.1f, 0.1f), new Vector2(0.1f, 0) };
        }

        public override void Update(GameTime gameTime)
        {
            var mousePos = Mouse.GetState().Position;
            var mouseLongLat = camera.GetLatLongOfCoord(new Vector2(mousePos.X, mousePos.Y));
            if (mouseLongLat != null)
            {
                Vector2 asVec2 = new Vector2((float)mouseLongLat.X, (float)mouseLongLat.Y);
                bool leftJustPressed = Mouse.GetState().LeftButton == ButtonState.Pressed && !oldLeft;
                bool rightJustPressed = Mouse.GetState().RightButton == ButtonState.Pressed && !oldRight;
                int bestIndex = -1;
                float bestDist = float.MaxValue;
                for (int i = 0; i < shape.Count; i++) // get closest line
                {
                    var p1 = shape[i];
                    var p2 = shape[(i + 1) % shape.Count];
                    float dist = AllMath.DistanceFromLineOrPoints(asVec2, p1, p2);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIndex = i;
                    }
                }
                float t = AllMath.ProjectionTOnLine(asVec2, shape[bestIndex], shape[(bestIndex + 1) % shape.Count]);
                if (t < 0.1)
                {
                    previewPoint = shape[bestIndex];
                    if (leftJustPressed) draggingPointIndex = bestIndex;
                }
                else if (t > 0.9)
                {
                    previewPoint = shape[(bestIndex + 1) % shape.Count];
                    if (leftJustPressed) draggingPointIndex = (bestIndex + 1) % shape.Count;
                }
                else
                {
                    previewPoint = shape[bestIndex] + t * (shape[(bestIndex + 1) % shape.Count] - shape[bestIndex]);
                    if (leftJustPressed)
                    {
                        shape.Insert(bestIndex + 1, previewPoint);
                        draggingPointIndex = bestIndex + 1;
                    }
                }
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    shape[draggingPointIndex] = asVec2;
                }
                if (Mouse.GetState().WasRightPressed() && Mouse.GetState().LeftButton != ButtonState.Pressed)
                {
                    if (t < 0.1)
                    {
                        shape.RemoveAt(bestIndex);
                    }
                    if (t > 0.9)
                    {
                        shape.RemoveAt((bestIndex + 1) % shape.Count);
                    }
                }
            }
            oldLeft = Mouse.GetState().LeftButton == ButtonState.Pressed;
            oldRight = Mouse.GetState().RightButton == ButtonState.Pressed;
        }

        public override void Draw(GameTime gameTime)
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            camera.ApplyMatrices(basicEffect3);
            foreach (var point in shape)
            {
                DrawPoint(point, Color.White);
            }
            var shapeAsVertices = shape.Select(x => new VertexPosition(Vector3Helper.UnitSphere(x.X, x.Y))).ToList();
            shapeAsVertices.Add(shapeAsVertices[0]);
            foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPosition>(PrimitiveType.LineStrip, shapeAsVertices.ToArray());
            }
            DrawPoint(previewPoint, Color.Red);
        }

        private void DrawPoint(Vector2 point, Color color)
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.VertexColorEnabled = true;
            basicEffect3.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1, 1000);
            List<VertexPositionColor> markerVertices = new List<VertexPositionColor>();
            Vector3 v = Vector3Helper.UnitSphere(point.X, point.Y);
            v = camera.Project(v);
            if (v.Z < 1)
            {
                float z = -10;
                markerVertices.Add(new VertexPositionColor(new Vector3(v.X - HALF_SIZE, v.Y + HALF_SIZE, z), color));
                markerVertices.Add(new VertexPositionColor(new Vector3(v.X - HALF_SIZE, v.Y - HALF_SIZE, z), color));
                markerVertices.Add(new VertexPositionColor(new Vector3(v.X + HALF_SIZE, v.Y + HALF_SIZE, z), color));
                markerVertices.Add(new VertexPositionColor(new Vector3(v.X + HALF_SIZE, v.Y - HALF_SIZE, z), color));
                foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, markerVertices.ToArray());
                }
            }
        }
    }
}

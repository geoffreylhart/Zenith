using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Helpers;
using Zenith.MathHelpers;

namespace Zenith.EditorGameComponents
{
    // assuming an Earth radius of 1
    public class CityMarker : DrawableGameComponent
    {
        private static int HALF_SIZE = 10;
        private EditorCamera camera;
        private String name;
        private double latitude;
        private double longitude;
        private SpriteBatch spriteBatch;

        public CityMarker(Game game, EditorCamera camera, String name, double latitude, double longitude) : base(game)
        {
            this.camera = camera;
            this.name = name;
            this.latitude = latitude;
            this.longitude = longitude;
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }

        public override void Draw(GameTime gameTime)
        {
            var basicEffect = new BasicEffect(GraphicsDevice);
            camera.ApplyMatrices(basicEffect);
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1, 1000);
            List<VertexPosition> markerVertices = new List<VertexPosition>();
            Vector3 v = Vector3Helper.UnitSphere(longitude * Mathf.PI / 180, latitude * Mathf.PI / 180);
            v = camera.Project(v);
            if (v.Z < 1)
            {
                float z = -10;
                markerVertices.Add(new VertexPosition(new Vector3(v.X - HALF_SIZE, v.Y + HALF_SIZE, z)));
                markerVertices.Add(new VertexPosition(new Vector3(v.X - HALF_SIZE, v.Y - HALF_SIZE, z)));
                markerVertices.Add(new VertexPosition(new Vector3(v.X + HALF_SIZE, v.Y + HALF_SIZE, z)));
                markerVertices.Add(new VertexPosition(new Vector3(v.X + HALF_SIZE, v.Y - HALF_SIZE, z)));
                foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives<VertexPosition>(PrimitiveType.TriangleStrip, markerVertices.ToArray());
                }
            }
            // city text
            Vector2 measured = GlobalContent.Arial.MeasureString(name);
            // apparently was setting the depthstencialstate to null and it would never get reset
            // this caused me so much confusion!
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(GlobalContent.Arial, name, new Vector2(v.X + HALF_SIZE * 1.5f, v.Y - measured.Y / 2), Color.White);
            spriteBatch.End();
        }
    }
}

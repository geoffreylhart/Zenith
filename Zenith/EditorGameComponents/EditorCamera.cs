using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;

namespace Zenith.EditorGameComponents
{
    public class EditorCamera : GameComponent, IUpdateable
    {
        public double cameraRotX = 0;
        public double cameraRotY = 0;
        public double cameraZoom = 0;
        private Matrix world;
        private Matrix view;
        private Matrix projection;

        public EditorCamera(Game game) : base(game)
        {
        }

        public override void Update(GameTime gameTime)
        {
            double cameraMoveAmount = -0.05 * Math.Pow(0.5, cameraZoom); // cheated and flipped the camera upside down
            Keyboard.GetState().AffectNumber(ref cameraRotX, Keys.Left, Keys.Right, Keys.A, Keys.D, cameraMoveAmount);
            Keyboard.GetState().AffectNumber(ref cameraRotY, Keys.Up, Keys.Down, Keys.W, Keys.S, cameraMoveAmount);
            Keyboard.GetState().AffectNumber(ref cameraZoom, Keys.Space, Keys.LeftShift, 0.01);
            world = Matrix.CreateRotationZ((float)cameraRotX) * Matrix.CreateRotationX((float)cameraRotY);
            // float distance = (float)(Math.Pow(0.5, cameraZoom) * 19);
            // TODO: cheated here and just turned the camera upside down
            view = Matrix.CreateLookAt(new Vector3(0, -20, 0), new Vector3(0, 0, 0), -Vector3.UnitZ); // we'll match Blender for "up" of camera and camera starting position
            // TODO: I just have no clue why the camera isn't working like it used to where I just move the camera closer and closer
            projection = Matrix.CreatePerspectiveFieldOfView((float)((Math.PI / 4) * Math.Pow(0.5, cameraZoom)), 800f / 480f, 0.1f, 100); // was 0.1f and 100f
        }

        internal void ApplyMatrices(BasicEffect basicEffect)
        {
            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;
        }

        internal Vector3 Project(Vector3 v)
        {
            return Game.GraphicsDevice.Viewport.Project(v, projection, view, world);
        }

        internal Vector3 Unproject(Vector3 v)
        {
            return Game.GraphicsDevice.Viewport.Unproject(v, projection, view, world);
        }
    }
}

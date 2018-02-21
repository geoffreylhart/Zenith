using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;

namespace Zenith.EditorGameComponents
{
    public class EditorCamera : GameComponent, IUpdateable
    {
        public double cameraRotX = 0;
        public double cameraRotY = 0;
        public double cameraZoom = 0;

        public EditorCamera(Game game) : base(game)
        {
        }

        public override void Update(GameTime gameTime)
        {
            double cameraMoveAmount = -0.05 * Math.Pow(0.5, cameraZoom); // cheated and flipped the camera upside down
            Keyboard.GetState().AffectNumber(ref cameraRotX, Keys.Left, Keys.Right, Keys.A, Keys.D, cameraMoveAmount);
            Keyboard.GetState().AffectNumber(ref cameraRotY, Keys.Up, Keys.Down, Keys.W, Keys.S, cameraMoveAmount);
            Keyboard.GetState().AffectNumber(ref cameraZoom, Keys.Space, Keys.LeftShift, 0.01);
        }
    }
}

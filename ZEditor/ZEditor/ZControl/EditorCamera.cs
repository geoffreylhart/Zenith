using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    class EditorCamera : AbstractCamera
    {
        private KeyboardState? prevKeyboardState = null;
        private MouseState? prevMouseState = null;

        public EditorCamera(Vector3 cameraPosition, Vector3 cameraTarget) : base(cameraPosition, cameraTarget)
        {
        }

        public override void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, GraphicsDevice graphicsDevice)
        {
        }
    }
}

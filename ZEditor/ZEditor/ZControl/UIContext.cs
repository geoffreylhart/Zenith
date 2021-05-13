using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZEditor.ZControl
{
    public class UIContext
    {
        private KeyboardState prevKeyboardState;
        private MouseState prevMouseState;
        private double elapsedSeconds = 0;
        private Game game;

        public AbstractCamera Camera { get; set; }

        public float AspectRatio { get { return game.GraphicsDevice.Viewport.AspectRatio; } }

        public double ScrollWheelDiff { get { return prevMouseState == null ? 0 : Mouse.GetState().ScrollWheelValue - prevMouseState.ScrollWheelValue; } }

        internal void CenterMouse()
        {
            Mouse.SetPosition(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
        }

        internal Vector2 MouseVector2 { get { return new Vector2(Mouse.GetState().X, Mouse.GetState().Y); } }

        public double ElapsedSeconds { get { return elapsedSeconds; } }

        public Vector2 MouseDiffVector2 { get { return prevMouseState == null ? new Vector2() : new Vector2(Mouse.GetState().X - prevMouseState.X, Mouse.GetState().Y - prevMouseState.Y); } }

        public Vector2 ScreenCenter { get { return new Vector2(game.GraphicsDevice.Viewport.Width / 2f, game.GraphicsDevice.Viewport.Height / 2f); } }

        public UIContext(Game game)
        {
            this.game = game;
        }

        public bool IsKeyPressed(Keys key)
        {
            if (prevKeyboardState == null) return false;
            return !prevKeyboardState.IsKeyDown(key) && Keyboard.GetState().IsKeyDown(key);
        }

        internal bool IsShiftPressed()
        {
            return Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift);
        }

        internal bool IsCtrlPressed()
        {
            return Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl);
        }

        public bool IsKeyShiftPressed(Keys key)
        {
            if (prevKeyboardState == null) return false;
            bool prevDown = prevKeyboardState.IsKeyDown(key) && (prevKeyboardState.IsKeyDown(Keys.LeftShift) || prevKeyboardState.IsKeyDown(Keys.RightShift));
            bool isDown = Keyboard.GetState().IsKeyDown(key) && (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift));
            return !prevDown && isDown;
        }

        public bool IsKeyCtrlPressed(Keys key)
        {
            if (prevKeyboardState == null) return false;
            bool prevDown = prevKeyboardState.IsKeyDown(key) && (prevKeyboardState.IsKeyDown(Keys.LeftControl) || prevKeyboardState.IsKeyDown(Keys.RightControl));
            bool isDown = Keyboard.GetState().IsKeyDown(key) && (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl));
            return !prevDown && isDown;
        }

        internal void WrapAroundMouse()
        {
            float w = game.GraphicsDevice.Viewport.Width;
            float h = game.GraphicsDevice.Viewport.Height;
            if (Mouse.GetState().X < 0 || Mouse.GetState().Y < 0 || Mouse.GetState().X > w || Mouse.GetState().Y > h)
            {
                Vector2 offsetAmount = new Vector2((Mouse.GetState().X + w) % w - Mouse.GetState().X, (Mouse.GetState().Y + h) % h - Mouse.GetState().Y);
                Mouse.SetPosition(Mouse.GetState().X + (int)offsetAmount.X, Mouse.GetState().Y + (int)offsetAmount.Y);
            }
        }

        internal Vector3 Project(Vector3 v, Matrix projection, Matrix view, Matrix world)
        {
            return game.GraphicsDevice.Viewport.Project(v, projection, view, world);
        }

        internal Vector3 Unproject(Vector3 v, Matrix projection, Matrix view, Matrix world)
        {
            return game.GraphicsDevice.Viewport.Unproject(v, projection, view, world);
        }

        internal void UpdateKeys()
        {
            prevKeyboardState = Keyboard.GetState();
            prevMouseState = Mouse.GetState();
        }

        internal void UpdateGameTime(GameTime gameTime)
        {
            elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        }

        internal bool IsLeftMouseButtonPressed()
        {
            return Mouse.GetState().LeftButton == ButtonState.Pressed && (prevMouseState == null || prevMouseState.LeftButton == ButtonState.Released);
        }

        internal bool IsRightMouseButtonPressed()
        {
            return Mouse.GetState().RightButton == ButtonState.Pressed && (prevMouseState == null || prevMouseState.RightButton == ButtonState.Released);
        }
    }
}

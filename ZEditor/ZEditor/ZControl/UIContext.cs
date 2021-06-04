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
        private IInputManager inputManager;
        private KeyboardState prevKeyboardState;
        private MouseState prevMouseState;
        private double elapsedSeconds = 0;
        private Game game;

        public UIContext(IInputManager inputManager, Game game)
        {
            this.inputManager = inputManager;
            this.game = game;
        }

        public AbstractCamera Camera { get; set; }

        public float AspectRatio { get { return game.GraphicsDevice.Viewport.AspectRatio; } }

        public double ScrollWheelDiff { get { return prevMouseState == null ? 0 : inputManager.GetMouseState().ScrollWheelValue - prevMouseState.ScrollWheelValue; } }

        public void CenterMouse()
        {
            Mouse.SetPosition(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
        }
        public void CheckListener(InputListener listener)
        {
            bool prevDown = listener.CheckMainDown(prevMouseState, prevKeyboardState);
            bool currDown = listener.CheckAllDown(inputManager.GetMouseState(), inputManager.GetKeyboardState());
            if(currDown && !prevDown)
            {
                listener.Trigger();
            }
        }

        public Vector2 MouseVector2 { get { return new Vector2(inputManager.GetMouseState().X, inputManager.GetMouseState().Y); } }

        public double ElapsedSeconds { get { return elapsedSeconds; } }

        public Vector2 MouseDiffVector2 { get { return prevMouseState == null ? new Vector2() : new Vector2(inputManager.GetMouseState().X - prevMouseState.X, inputManager.GetMouseState().Y - prevMouseState.Y); } }

        public Vector2 ScreenCenter { get { return new Vector2(game.GraphicsDevice.Viewport.Width / 2f, game.GraphicsDevice.Viewport.Height / 2f); } }

        public void WrapAroundMouse()
        {
            float w = game.GraphicsDevice.Viewport.Width;
            float h = game.GraphicsDevice.Viewport.Height;
            if (inputManager.GetMouseState().X < 0 || inputManager.GetMouseState().Y < 0 || inputManager.GetMouseState().X > w || inputManager.GetMouseState().Y > h)
            {
                Vector2 offsetAmount = new Vector2((inputManager.GetMouseState().X + w) % w - inputManager.GetMouseState().X, (inputManager.GetMouseState().Y + h) % h - inputManager.GetMouseState().Y);
                Mouse.SetPosition(inputManager.GetMouseState().X + (int)offsetAmount.X, inputManager.GetMouseState().Y + (int)offsetAmount.Y);
            }
        }

        public Vector3 Project(Vector3 v, Matrix projection, Matrix view, Matrix world)
        {
            return game.GraphicsDevice.Viewport.Project(v, projection, view, world);
        }

        public Vector3 Unproject(Vector3 v, Matrix projection, Matrix view, Matrix world)
        {
            return game.GraphicsDevice.Viewport.Unproject(v, projection, view, world);
        }

        public void UpdateKeys()
        {
            prevKeyboardState = inputManager.GetKeyboardState();
            prevMouseState = inputManager.GetMouseState();
        }

        public void UpdateGameTime(GameTime gameTime)
        {
            elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Exit()
        {
            game.Exit();
        }
    }
}

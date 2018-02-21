using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents
{
    public class DebugConsole : DrawableGameComponent
    {
        private SpriteFont font;
        private String strBuffer = "";
        private SpriteBatch spriteBatch;

        public DebugConsole(Game game) : base(game)
        {
            font = game.Content.Load<SpriteFont>("Arial");
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }
        
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            spriteBatch.DrawString(font, strBuffer, new Vector2(5, 5), Color.White);
            spriteBatch.End();
            strBuffer = "";
        }
        
        public void Debug(Object str)
        {
            if (strBuffer.Length > 0) strBuffer += "\n";
            strBuffer += str;
        }
    }
}

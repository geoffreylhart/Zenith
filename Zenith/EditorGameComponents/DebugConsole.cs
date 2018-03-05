using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents
{
    public class DebugConsole : DrawableGameComponent
    {
        private SpriteFont font;
        private String strBuffer = "";
        private String strSet = "";
        private SpriteBatch spriteBatch;

        public DebugConsole(Game game) : base(game)
        {
            font = game.Content.Load<SpriteFont>("Arial");
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }

        public override void Draw(GameTime gameTime)
        {
            // apparently was setting the depthstencialstate to null and it would never get reset
            // this caused me so much confusion!
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(font, strSet + "\n" + strBuffer, new Vector2(5, 5), Color.White);
            spriteBatch.End();
            strBuffer = "";
        }

        public void Debug(Object str)
        {
            if (strBuffer.Length > 0) strBuffer += "\n";
            strBuffer += str;
        }

        public void DebugSet(Object str)
        {
            strSet = str.ToString();
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.ZGame;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents
{
    public class DebugConsole : ZGameComponent
    {
        private String strBuffer = "";
        private String strSet = "";
        private SpriteBatch spriteBatch;

        public DebugConsole(Game game)
        {
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }

        public override void Draw(RenderContext renderContext, GameTime gameTime)
        {
            if (renderContext.layerPass != RenderContext.LayerPass.UI_PASS) return;
            // apparently was setting the depthstencialstate to null and it would never get reset
            // this caused me so much confusion!
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(GlobalContent.Arial, strSet + "\n" + strBuffer, new Vector2(5, 5), Color.White);
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

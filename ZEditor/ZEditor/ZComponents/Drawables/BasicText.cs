using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.Drawables
{
    public class BasicText : ZComponent
    {
        private static SpriteBatch spriteBatch;
        public Vector2 position;
        public string text;

        public override void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            if (text != null)
            {
                if (spriteBatch == null) spriteBatch = new SpriteBatch(graphics);
                Vector2 measured = GlobalContent.Arial.MeasureString(text);
                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
                spriteBatch.DrawString(GlobalContent.Arial, text, position, Color.White);
                spriteBatch.End();
            }
        }
    }
}

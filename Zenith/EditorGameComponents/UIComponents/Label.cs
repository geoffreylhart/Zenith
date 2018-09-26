using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    internal class Label : IUIComponent
    {
        public int W { get; set; }
        public int H { get; set; }

        public void Draw(GraphicsDevice graphicsDevice, int x, int y)
        {
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            spriteBatch.DrawString(GlobalContent.Arial, GetText(), new Vector2(x, y), Color.White);
            spriteBatch.End();
        }

        public void Update(int x, int y)
        {
        }

        virtual internal string GetText()
        {
            return "";
        }
    }
}

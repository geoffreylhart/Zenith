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
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        public Label(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public void Draw(GraphicsDevice graphicsDevice)
        {
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            spriteBatch.DrawString(GlobalContent.Arial, GetText(), new Vector2(X, Y), Color.White);
            spriteBatch.End();
        }

        public void Update()
        {
        }

        virtual internal string GetText()
        {
            return "";
        }
    }
}

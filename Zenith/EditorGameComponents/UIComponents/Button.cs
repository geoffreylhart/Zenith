using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    class Button : IUIComponent
    {
        private SpriteFont FONT { get { return GlobalContent.Arial; } }
        private static int PADDING = 5;

        public int W { get; set; }
        public int H { get; set; }
        internal Action OnClick;
        private string name;
        private bool hovering = false;

        public Button(string name)
        {
            this.name = name;
        }

        public void Draw(GraphicsDevice graphicsDevice, int x, int y)
        {
            Vector2 boxSize = FONT.MeasureString(name) + new Vector2(PADDING * 2, PADDING * 2);
            GraphicsBasic.DrawRect(graphicsDevice, x, y, boxSize.X, boxSize.Y, hovering ? Color.Cyan : Color.White);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            spriteBatch.DrawString(FONT, name, new Vector2(x + PADDING, y + PADDING), Color.Black);
            spriteBatch.End();
        }

        public void Update(int x, int y)
        {
            Vector2 boxSize = FONT.MeasureString(name) + new Vector2(PADDING * 2, PADDING * 2);
            int mouseX = Mouse.GetState().X;
            int mouseY = Mouse.GetState().Y;
            hovering = false;
            if (mouseX >= x && mouseX <= x + boxSize.X && mouseY >= y && mouseY <= y + boxSize.Y)
            {
                if (UILayer.LeftPressed)
                {
                    OnClick();
                }
                hovering = true;
                UILayer.ConsumeLeft();
            }
        }
    }
}

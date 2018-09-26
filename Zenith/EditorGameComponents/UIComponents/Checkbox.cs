﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    class Checkbox : IUIComponent
    {
        private SpriteFont FONT { get { return GlobalContent.Arial; } }
        private static int PADDING = 5;

        public int W { get; set; }
        public int H { get; set; }
        public virtual bool Enabled { get; set; }
        private string text;

        public Checkbox(string text)
        {
            this.text = text;
        }

        public void Draw(GraphicsDevice graphicsDevice, int x, int y)
        {
            float boxSize = FONT.MeasureString(text).Y;
            GraphicsBasic.DrawRect(graphicsDevice, x, y, boxSize, boxSize, Color.Black);
            GraphicsBasic.DrawRect(graphicsDevice, x + 2, y + 2, boxSize - 4, boxSize - 4, Color.White);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            spriteBatch.DrawString(FONT, text, new Vector2(x + boxSize + PADDING, y), Color.White);
            if (Enabled)
            {
                float offset = (boxSize - FONT.MeasureString("X").X) / 2;
                spriteBatch.DrawString(FONT, "X", new Vector2(x + offset, y), Color.Black);
            }
            spriteBatch.End();
        }

        public void Update(int x, int y)
        {
            float boxSize = FONT.MeasureString(text).Y;
            int mouseX = Mouse.GetState().X;
            int mouseY = Mouse.GetState().Y;
            if (mouseX >= x && mouseX <= x + boxSize && mouseY >= y && mouseY <= y + boxSize)
            {
                if (UILayer.LeftPressed)
                {
                    Enabled = !Enabled;
                }
                UILayer.ConsumeLeft();
            }
        }
    }
}

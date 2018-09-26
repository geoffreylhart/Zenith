using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Zenith.EditorGameComponents.UIComponents
{
    class Tabs : IUIComponent
    {
        private SpriteFont TITLE_FONT { get { return GlobalContent.ArialBold; } }
        private static int TITLE_PADDING = 10;
        private static int TITLE_PADDING_BOTTOM = 5;
        private static int TAB_OFFSET = 25;
        private static int TAB_DIST = 5; // distance between tabs

        public int W { get; set; }
        public int H { get; set; }

        private int tabCount;
        internal List<Panel> panels;
        internal List<string> titles;
        private int activeIndex = 0;
        internal int hoverIndex = -1;

        public Tabs(int tabCount, int w, int h)
        {
            this.tabCount = tabCount;
            panels = new List<Panel>();
            titles = new List<string>();
            for (int i = 0; i <= tabCount; i++)
            {
                Panel newPanel = new Panel();
                panels.Add(newPanel);
                titles.Add("");
            }
            this.W = w;
            this.H = h;
        }

        // we want the blur from the front-most tab to overlay the rest, at least
        // I want to somehow avoid weird double-glow overlap, if that's even an issue (I think it might not be, provided you blur using exactly w/e the "extra" tab shape is)
        // this means you have to be careful about offsetting the shape, I think

        public void Draw(GraphicsDevice graphicsDevice, int x, int y)
        {
            int textHeight = (int)TITLE_FONT.MeasureString(titles[0]).Y;
            int[] tabXPos = new int[tabCount];
            tabXPos[0] = x + TAB_OFFSET;
            for (int i = 1; i < tabCount; i++)
            {
                tabXPos[i] = tabXPos[i - 1] + 2 * TITLE_PADDING + (int)TITLE_FONT.MeasureString(titles[i - 1]).X + TAB_DIST;
            }
            for (int i = 0; i < tabCount; i++)
            {
                if (i != activeIndex)
                {
                    int strW = (int)TITLE_FONT.MeasureString(titles[i]).X;
                    UITemp.DrawBackTab(tabXPos[i], y, strW + 2 * TITLE_PADDING, textHeight + TITLE_PADDING + TITLE_PADDING_BOTTOM, i == hoverIndex ? new Color(0, 180, 255) : new Color(0, 90, 128), graphicsDevice); // unlike most, we'll have y be the bottom of the tab
                }
            }
            int activeStrW = (int)TITLE_FONT.MeasureString(titles[activeIndex]).X;
            UITemp.DrawFrontTab(x, y, W, H, tabXPos[activeIndex] - x, activeStrW + 2 * TITLE_PADDING, textHeight + TITLE_PADDING + TITLE_PADDING_BOTTOM, graphicsDevice, Game1.renderTarget);
            panels[activeIndex].Draw(graphicsDevice, x, y);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            int textOffset = 0;
            for (int i = 0; i < tabCount; i++)
            {
                spriteBatch.DrawString(TITLE_FONT, titles[i], new Vector2(tabXPos[i] + TITLE_PADDING, y - textHeight - TITLE_PADDING_BOTTOM), Color.White);
                textOffset += 2 * TITLE_PADDING + (int)TITLE_FONT.MeasureString(titles[i]).X;
            }
            spriteBatch.End();
        }

        public void Update(int x, int y)
        {
            panels[activeIndex].Update(x, y);
            int textHeight = (int)TITLE_FONT.MeasureString(titles[0]).Y;
            hoverIndex = -1;
            int mouseX = Mouse.GetState().X;
            int mouseY = Mouse.GetState().Y;
            if (mouseY >= y - textHeight - TITLE_PADDING - TITLE_PADDING_BOTTOM && mouseY <= y)
            {
                int tabXPos = x + TAB_OFFSET;
                for (int i = 0; i < tabCount; i++)
                {
                    int tabW = (int)TITLE_FONT.MeasureString(titles[i]).X + 2 * TITLE_PADDING;
                    if (mouseX >= tabXPos && mouseX <= tabXPos + tabW)
                    {
                        hoverIndex = i;
                        if (UILayer.LeftPressed) activeIndex = i;
                    }
                    tabXPos += tabW + TAB_DIST;
                }
                if (mouseX >= x + TAB_OFFSET && mouseX <= tabXPos - TAB_DIST) UILayer.ConsumeLeft();
            }
            if (mouseX >= x && mouseX <= x + W && mouseY >= y && mouseY <= y + H) UILayer.ConsumeLeft();
        }
    }
}

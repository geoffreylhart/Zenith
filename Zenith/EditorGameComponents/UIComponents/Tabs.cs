using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    class Tabs : IUIComponent
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        private int tabCount;
        internal List<Panel> panels;
        internal List<string> titles;
        private int activeIndex = 0;

        public Tabs(int tabCount, int x, int y, int w, int h)
        {
            this.tabCount = tabCount;
            panels = new List<Panel>();
            titles = new List<string>();
            for (int i = 0; i <= tabCount; i++)
            {
                Panel newPanel = new Panel();
                newPanel.X = x;
                newPanel.Y = y;
                panels.Add(newPanel);
                titles.Add("");
            }
            this.X = x;
            this.Y = y;
            this.W = w;
            this.H = h;
        }

        public void Draw(GraphicsDevice graphicsDevice)
        {
            UITemp.DrawThoseTabs(X, Y, W, H, graphicsDevice, Game1.renderTarget);
            panels[activeIndex].Draw(graphicsDevice);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            for (int i = 0; i < tabCount; i++)
            {
                spriteBatch.DrawString(GlobalContent.ArialBold, titles[i], new Vector2(X + 45, Y - 20), Color.White);
            }
            spriteBatch.End();
        }

        public void Update()
        {
        }
    }
}

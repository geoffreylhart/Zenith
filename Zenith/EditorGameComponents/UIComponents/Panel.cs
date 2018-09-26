using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    internal class Panel : IUIComponent
    {
        private static int PADDING = 5;

        public int W { get; set; }
        public int H { get; set; }
        public virtual List<IUIComponent> Components { get; set; }

        public Panel()
        {
            Components = new List<IUIComponent>();
        }

        public void Draw(GraphicsDevice graphicsDevice, int x, int y)
        {
            int newy = y + PADDING;
            for (int i = 0; i < Components.Count; i++)
            {
                Components[i].Draw(graphicsDevice, x + PADDING, newy);
                newy += Components[i].H + PADDING * 2;
            }
        }

        public void Update(int x, int y)
        {
            int newy = y + PADDING;
            for (int i = 0; i < Components.Count; i++)
            {
                Components[i].Update(x + PADDING, newy);
                newy += Components[i].H + PADDING * 2;
            }
        }
    }
}

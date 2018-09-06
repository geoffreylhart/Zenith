using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    class ListBox<T> : IUIComponent
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        internal int activeIndex = 0;
        private List<T> items;

        public ListBox(int x, int y, int w, List<T> items)
        {
            this.X = x;
            this.Y = y;
            this.W = w;
            this.items = items;
        }

        public void Draw(GraphicsDevice graphicsDevice)
        {
        }

        public void Update()
        {
        }

        internal virtual string GetItemAsString(T item)
        {
            return item.ToString();
        }
    }
}

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

        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        private List<IUIComponent> components = new List<IUIComponent>();

        public void Draw(GraphicsDevice graphicsDevice)
        {
            foreach (var component in components) component.Draw(graphicsDevice);
        }

        public void Update()
        {
        }

        internal void Add(IUIComponent component)
        {
            if (components.Count == 0)
            {
                component.X = X + PADDING;
                component.Y = Y + PADDING;
            }
            else
            {
                component.X = X + PADDING;
                component.Y = components[components.Count - 1].Y + PADDING;
            }
            components.Add(component);
        }
    }
}

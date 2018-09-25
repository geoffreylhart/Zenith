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
        public virtual List<IUIComponent> Components { get; set; }

        public Panel()
        {
            Components = new List<IUIComponent>();
        }

        public void Draw(GraphicsDevice graphicsDevice)
        {
            foreach (var component in Components) component.Draw(graphicsDevice);
        }

        private void ResetPositions()
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (i == 0)
                {
                    Components[i].X = X + PADDING;
                    Components[i].Y = Y + PADDING;
                }
                else
                {
                    Components[i].X = X + PADDING;
                    Components[i].Y = Components[Components.Count - 1].Y + PADDING;
                }
            }
        }

        public void Update()
        {
            ResetPositions();
            foreach(var component in Components) component.Update();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    class Button : IUIComponent
    {
        public int W { get; set; }
        public int H { get; set; }

        public void Draw(GraphicsDevice graphicsDevice, int x, int y)
        {
            throw new NotImplementedException();
        }

        public void Update(int x, int y)
        {
            throw new NotImplementedException();
        }
    }
}

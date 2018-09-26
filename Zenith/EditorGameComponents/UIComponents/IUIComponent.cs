using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.EditorGameComponents.UIComponents
{
    interface IUIComponent
    {
        int W { get; set; }
        int H { get; set; }
        void Draw(GraphicsDevice graphicsDevice, int x, int y);
        void Update(int x, int y);
    }
}

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.EditorGameComponents.FlatComponents
{
    interface IFlatComponent
    {
        void Draw(GraphicsDevice graphicsDevice, double minX, double maxX, double minY, double maxY, double cameraZoom);
        void Update(double mouseX, double mouseY, double cameraZoom);
    }
}

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    interface IFlatComponent
    {
        // don't actually draw anything to the caller, just initialize your own internal texture draws (only call this once per frame)
        void InitDraw(GraphicsDevice graphicsDevice, ISector rootSector, double minLong, double maxLong, double minLat, double maxLat, double cameraZoom);
        // draw to the caller
        void Draw(GraphicsDevice graphicsDevice, ISector rootSector, double minLong, double maxLong, double minLat, double maxLat, double cameraZoom);
        void Update(double mouseX, double mouseY, double cameraZoom);
    }
}

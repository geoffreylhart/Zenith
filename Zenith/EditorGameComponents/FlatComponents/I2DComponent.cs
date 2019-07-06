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
        void Draw(RenderTarget2D renderTarget, ISector rootSector, double minLong, double maxLong, double minLat, double maxLat, double cameraZoom);
        void Update(double mouseX, double mouseY, double cameraZoom);
    }
}

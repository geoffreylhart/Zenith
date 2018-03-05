using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Zenith.EditorGameComponents;

namespace Zenith.Helpers
{
    public static class GameComponentHelper
    {
        internal static DebugConsole GetDebugConsole(this GameComponent gameComponent)
        {
            return ((Game1)gameComponent.Game).debug;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.EditorGameComponents;

namespace Zenith.Helpers
{
    public static class GameComponentHelper
    {
        internal static DebugConsole GetDebugConsole(this GameComponent gameComponent)
        {
            return ((Game1)gameComponent.Game).debug;
        }

        internal static BasicEffect GetDefaultEffect(this GameComponent gameComponent)
        {
            var basicEffect = new BasicEffect(gameComponent.Game.GraphicsDevice);
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            return basicEffect;
        }
    }
}

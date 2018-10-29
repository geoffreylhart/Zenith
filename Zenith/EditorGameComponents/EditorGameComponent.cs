using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.EditorGameComponents.UIComponents;

namespace Zenith.EditorGameComponents
{
    public abstract class EditorGameComponent : DrawableGameComponent
    {
        protected EditorGameComponent(Game game) : base(game)
        {
        }

        internal virtual List<String> GetDebugInfo() { return new List<String>(); }

        internal virtual List<IUIComponent> GetSettings() { return new List<IUIComponent>(); }

        protected BasicEffect GetDefaultEffect()
        {
            var basicEffect = new BasicEffect(Game.GraphicsDevice);
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1, 1, 1);
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            return basicEffect;
        }
    }
}

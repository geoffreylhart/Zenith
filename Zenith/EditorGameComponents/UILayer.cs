using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.Helpers;

// TODO: likely split up the logic for the UI layer across multiple classes
// a component manager, which exclusively adds buttons and stuff and turns components on and off
//      - it might not actually attach as a component!
// a ui manager, which actually draws buttons and handles keys/mouse events but doesnt care about the game
// button, pane, scrollpane etc, are all components and will probably control their own drawing and let us know when they swallow key events
// some separate class that well let us render vector images and blur effects
// potentially geometry/path classes, which can turn themselves into meshes (instead of our drawing class having to do that)
namespace Zenith.EditorGameComponents
{
    // For now, only creates a side menu for clicking between different components
    // Doesn't handle transperency or anything
    // Only toggles visibility and such
    internal class UILayer : DrawableGameComponent
    {
        private List<IUIComponent> components = new List<IUIComponent>();
        private ComponentManager cm;

        internal UILayer(Game game, ComponentManager cm) : base(game)
        {
            this.cm = cm;
            cm.Init(this);
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteBatch.Begin();
            spriteBatch.Draw(Game1.renderTarget, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);
            spriteBatch.End();
            foreach (var component in components)
            {
                component.Draw(GraphicsDevice);
            }
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var component in components)
            {
                component.Update();
            }
        }

        internal void Add(IUIComponent component)
        {
            components.Add(component);
        }
    }
}
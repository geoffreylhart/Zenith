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
        private List<ComponentCoord> components = new List<ComponentCoord>();
        private ComponentManager cm;
        private static bool oldLeft = false;
        private static bool oldRight = false;
        internal static bool LeftDown { get; private set; }
        internal static bool RightDown { get; private set; }
        internal static bool LeftPressed { get; private set; }
        internal static bool RightPressed { get; private set; }

        internal static void ConsumeLeft()
        {
            LeftDown = false;
            LeftPressed = false;
        }

        internal static void ConsumeRight()
        {
            RightDown = false;
            RightPressed = false;
        }

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
            // TODO: prevent other components (like Google Maps) from registering clicks when the mouse clicks in certain areas (directly on buttons or even just near them)
            // what's the best way to handle this?
            // for instance, should be handle the new component click actions within that component's update step? or in our UIs?
            // if we update in the component's step, we get to pretend we're doing good by piggy-backing off monogame's update order stuff (instead of having our own)
            MouseState state = Mouse.GetState();
            LeftDown = state.LeftButton == ButtonState.Pressed;
            RightDown = state.LeftButton == ButtonState.Pressed;
            LeftPressed = state.LeftButton == ButtonState.Pressed && !oldLeft;
            oldLeft = state.LeftButton == ButtonState.Pressed;
            RightPressed = state.RightButton == ButtonState.Pressed && !oldRight;
            oldRight = state.RightButton == ButtonState.Pressed;
            foreach (var component in components)
            {
                component.Update();
            }
        }

        internal void Add(IUIComponent component, int x, int y)
        {
            components.Add(new ComponentCoord(component, x, y));
        }
    }

    internal class ComponentCoord
    {
        private IUIComponent component;
        private int x;
        private int y;

        public ComponentCoord(IUIComponent component, int x, int y)
        {
            this.component = component;
            this.x = x;
            this.y = y;
        }

        internal void Draw(GraphicsDevice graphicsDevice)
        {
            component.Draw(graphicsDevice, x, y);
        }

        internal void Update()
        {
            component.Update(x, y);
        }
    }
}
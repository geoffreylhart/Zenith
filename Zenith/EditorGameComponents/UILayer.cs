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
        private static int QUICK_CLICK_MAX_FRAMES = 10; // where 1 is minimum and 0 makes quick clicks impossible
        private List<ComponentCoord> components = new List<ComponentCoord>();
        private static bool oldLeft = false;
        private static bool oldRight = false;
        private static int leftAge = 0;
        private static int rightAge = 0;
        internal static bool LeftDown { get; private set; }
        internal static bool RightDown { get; private set; }
        internal static bool LeftPressed { get; private set; }
        internal static bool RightPressed { get; private set; }
        internal static bool LeftQuickClicked { get; private set; }
        internal static bool RightQuickClicked { get; private set; }
        internal static bool LeftAvailable { get; private set; }
        internal static bool RightAvailable { get; private set; }

        internal static void ConsumeLeft()
        {
            // don't set leftdown to false so components can still register dragging?
            LeftPressed = false;
            LeftQuickClicked = false;
            LeftAvailable = false;
        }

        internal static void ConsumeRight()
        {
            // don't set rightdown to false so components can still register dragging?
            RightPressed = false;
            RightQuickClicked = false;
            RightAvailable = false;
        }

        internal UILayer(Game game) : base(game)
        {
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle screenRect = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: how come we have to use position in our shader and can't user the texture coordinate anymore??
            GlobalContent.SSAOShader.Parameters["ScreenSize"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            GlobalContent.FXAAShader.Parameters["ScreenSize"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            //GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            //GraphicsDevice.SetRenderTarget(Game1.renderTargets[3]);
            //spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, GlobalContent.SSAOShader);
            //spriteBatch.Draw(Game1.renderTargets[2], screenRect, screenRect, Color.White);
            //spriteBatch.End();

            //GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            //GraphicsDevice.SetRenderTarget(Game1.renderTargets[4]);
            //spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, GlobalContent.FXAAShader);
            //spriteBatch.Draw(Game1.renderTargets[3], screenRect, screenRect, Color.White);
            //spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            spriteBatch.Begin();
            spriteBatch.Draw(Game1.ALBEDO_BUFFER, screenRect, Color.White);
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
            LeftAvailable = true;
            RightAvailable = true;
            LeftDown = state.LeftButton == ButtonState.Pressed;
            RightDown = state.RightButton == ButtonState.Pressed;
            LeftPressed = LeftDown && !oldLeft;
            LeftQuickClicked = !LeftDown && oldLeft && leftAge < QUICK_CLICK_MAX_FRAMES;
            if (oldLeft == LeftDown)
            {
                leftAge++;
            }
            else
            {
                leftAge = 0;
            }
            oldLeft = LeftDown;
            RightPressed = RightDown && !oldRight;
            RightQuickClicked = !RightDown && oldRight && rightAge < QUICK_CLICK_MAX_FRAMES;
            if (oldRight == RightDown)
            {
                rightAge++;
            }
            else
            {
                rightAge = 0;
            }
            oldRight = RightDown;
            foreach (var component in components)
            {
                component.Update();
            }
        }

        internal void Add(IUIComponent component, int x, int y)
        {
            //components.Add(new ComponentCoord(component, x, y));
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;

namespace Zenith.EditorGameComponents
{
    // For now, only creates a side menu for clicking between different components
    // Doesn't handle transperency or anything
    // Only toggles visibility and such
    public class ComponentManager : DrawableGameComponent
    {
        private GameComponent[] components;
        private int activeIndex = 0;
        private int hoverIndex = -1;
        private bool isPressed = false;
        private SpriteFont font;
        private SpriteBatch spriteBatch;

        public ComponentManager(Game game, params GameComponent[] components) : base(game)
        {
            this.components = components;
            foreach (var c in components)
            {
                c.Enabled = false;
            }
            components[0].Enabled = true;
            font = game.Content.Load<SpriteFont>("Arial");
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }
        public override void Draw(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            for (int i = 0; i < components.Length; i++)
            {
                int x = GraphicsDevice.Viewport.Width - 210;
                int y = 10 + 30 * i;
                bool hoveredOver = i == hoverIndex;
                Color color;
                if (i == activeIndex)
                {
                    color = hoveredOver ? (isPressed ? new Color(255, 60, 60) : new Color(255, 100, 100)) : new Color(255, 80, 80);
                }
                else
                {
                    color = hoveredOver ? (isPressed ? new Color(60, 60, 255) : new Color(100, 100, 255)) : new Color(80, 80, 255);
                }
                DrawButton(components[i].GetType().Name, color, x, y, 200, 25);
            }
        }

        private void DrawButton(String text, Color color, float x, float y, float w, float h)
        {
            float z = -10f;
            List<VertexPositionColor> rect = new List<VertexPositionColor>();
            rect.Add(new VertexPositionColor(new Vector3(x, y + h, z), color)); // bottom-left
            rect.Add(new VertexPositionColor(new Vector3(x, y, z), color)); // top-left
            rect.Add(new VertexPositionColor(new Vector3(x + w, y + h, z), color)); // bottom-right
            rect.Add(new VertexPositionColor(new Vector3(x + w, y, z), color)); // top-right
            var basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1, 1000);
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, rect.ToArray());
            }
            Vector2 measured = font.MeasureString(text);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(font, text, new Vector2(x + (w - measured.X) / 2, y + (h - measured.Y) / 2), Color.White);
            spriteBatch.End();
        }


        bool oldLeft = false;
        public override void Update(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            isPressed = Mouse.GetState().LeftButton == ButtonState.Pressed;
            hoverIndex = -1;
            for (int i = 0; i < components.Length; i++)
            {
                int x = GraphicsDevice.Viewport.Width - 210;
                int y = 10 + 30 * i;
                bool hoveredOver = mouseState.X >= x && mouseState.X <= (x + 200) && mouseState.Y > y && mouseState.Y < y + 25;
                if (hoveredOver) hoverIndex = i;
                if (hoveredOver && Mouse.GetState().LeftButton == ButtonState.Released && oldLeft)
                {
                    components[activeIndex].Enabled = false;
                    activeIndex = i;
                }
            }
            // let's not try to figure out these off-by-one issues regarding disabling components -right- before they register
            if (!components[activeIndex].Enabled && hoverIndex == -1) components[activeIndex].Enabled = true;
            if (hoverIndex != -1) components[activeIndex].Enabled = false;
            oldLeft = Mouse.GetState().LeftButton == ButtonState.Pressed;
        }
    }
}

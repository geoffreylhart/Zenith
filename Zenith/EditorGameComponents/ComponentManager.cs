using System;
using System.Collections.Generic;
using System.IO;
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
        private SpriteBatch spriteBatch;
        private Texture2D blankTexture;

        public ComponentManager(Game game, params GameComponent[] components) : base(game)
        {
            this.components = components;
            foreach (var c in components)
            {
                c.Enabled = false;
            }
            components[0].Enabled = true;
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
            //using (var fileStream = new FileStream(@"C:\Users\Geoffrey Hart\Documents\Visual Studio 2017\Projects\Factorio\FactoryPlanner\FactoryPlanner\Images\Icons\blank.png", FileMode.Open))
            //{
            //    blankTexture = Texture2D.FromStream(game.GraphicsDevice, fileStream);
            //}
        }
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(((Game1)Game).renderTarget, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);
            spriteBatch.End();
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
            //
            int padding = 5;
            //DrawRect(20 - padding, GraphicsDevice.Viewport.Height - 150 - padding, 500 + padding * 2, 200 + padding, Color.Red);
            //DrawBlur(20, GraphicsDevice.Viewport.Height - 150, 500, 200, Color.Red);
            //DrawTabs(20, GraphicsDevice.Viewport.Height - 150, 500, 200, Color.Blue, "Properties", "Debug", "Other Stuff");
            UITemp.DrawThoseTabs(20, GraphicsDevice.Viewport.Height - 150, 500, 200, GraphicsDevice, ((Game1)Game).renderTarget);
        }

        private void DrawTabs(int x, int y, int w, int h, Color tabColor, params String[] tabNames)
        {
            Color textColor = Color.White;
            //DrawBlur(x, y, w, h, Color.Red);
            int cornerRadius = 15;
            int cornerRes = 15;
            int tabRes = 15;
            float lineThickness = 2;
            float tabSideWidth = 20;
            float tabHeight = GlobalContent.Arial.MeasureString(" ").Y;
            float[] namePos = new float[tabNames.Length];
            float tabPadding = 5;
            float nameTempPos = x + tabSideWidth + tabPadding + cornerRadius;
            for (int i = 0; i < tabNames.Length; i++)
            {
                namePos[i] = nameTempPos;
                nameTempPos += GlobalContent.Arial.MeasureString(tabNames[i]).X + 2 * tabSideWidth + 2 * tabPadding;
            }
            var basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1, 1000);
            for (int i = 2; i >= 0; i--)
            {
                List<VertexPositionColor> tab = new List<VertexPositionColor>();
                for (int j = 0; j < tabRes * 2; j++)
                {
                    float height = TabCurve(j < tabRes ? j / (tabRes - 1f) : (tabRes * 2 - 1 - j) / (tabRes - 1f)) * (tabHeight + tabPadding * 2);
                    float pos;
                    if (j < tabRes)
                    {
                        pos = namePos[i] - tabSideWidth - tabPadding + j * tabSideWidth / (tabRes - 1f);
                    }
                    else
                    {
                        pos = namePos[i] + GlobalContent.Arial.MeasureString(tabNames[i]).X + tabPadding + (j - tabRes) * tabSideWidth / (tabRes - 1f);
                    }
                    tab.Add(new VertexPositionColor(new Vector3(pos, y, -10f), tabColor));
                    tab.Add(new VertexPositionColor(new Vector3(pos, y - height, -10f), tabColor));
                }
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, tab.ToArray());
                }
            }
            spriteBatch.Begin();
            for (int i = 0; i < tabNames.Length; i++)
            {
                spriteBatch.DrawString(GlobalContent.Arial, tabNames[i], new Vector2(namePos[i], y - tabHeight - tabPadding), textColor);
            }
            spriteBatch.End();
            // do the rounded box
            List<VertexPositionColor> box = new List<VertexPositionColor>();
            for (int j = 0; j < cornerRes * 2; j++)
            {
                float temp = (j < cornerRes ? j / (cornerRes - 1f) : (cornerRes * 2 - 1 - j) / (cornerRes - 1f));
                float height = cornerRadius * (1 - (float)Math.Sqrt(1 - (1 - temp) * (1 - temp)));
                float pos;
                if (j < cornerRes)
                {
                    pos = x + j * cornerRadius / (cornerRes - 1f);
                }
                else
                {
                    pos = x + w - cornerRadius + (j - cornerRes) * cornerRadius / (cornerRes - 1f);
                }
                box.Add(new VertexPositionColor(new Vector3(pos, y + h - height, -10f), tabColor));
                box.Add(new VertexPositionColor(new Vector3(pos, y + height, -10f), tabColor));
            }
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, box.ToArray());
            }
            //DrawBlur(x + cornerRadius / 2, y + cornerRadius / 2, w - cornerRadius / 2 * 2, h - cornerRadius / 2 * 2, Color.White);
            GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, GraphicsDevice.Viewport.MaxDepth, 0);
            DrawRect(x + cornerRadius / 2, y + cornerRadius / 2, w - cornerRadius / 2 * 2, h - cornerRadius / 2 * 2, new Color(tabColor, 0.5f));
        }

        private float TabCurve(float x)
        {
            //return x;
            // integral of 1-(2x-1)^2, then scaled up
            //return -6 * (x * x * x / 3 - x * x / 2);
            // just couple 2 parabolas
            if (x < 0.5) return 2 * x * x;
            return 1 - 2 * (x - 1) * (x - 1);
        }

        private void DrawRect(int x, int y, int w, int h, Color color)
        {
            float z = -10f;
            List<VertexPositionColor> rect = new List<VertexPositionColor>();
            rect.Add(new VertexPositionColor(new Vector3(x, y + h, z), color)); // bottom-left
            rect.Add(new VertexPositionColor(new Vector3(x, y, z), color)); // top-left
            rect.Add(new VertexPositionColor(new Vector3(x + w, y + h, z), color)); // bottom-right
            rect.Add(new VertexPositionColor(new Vector3(x + w, y, z), color)); // top-right
            var basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Alpha = color.A / 255f;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1, 1000);
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, rect.ToArray());
            }
        }

        //    private void DrawBlur(int x, int y, int w, int h, Color color)
        //    {
        //        float z = -10f;
        //        List<VertexPositionColor> rect = new List<VertexPositionColor>();
        //        rect.Add(new VertexPositionColor(new Vector3(x, y + h, z), color)); // bottom-left
        //        rect.Add(new VertexPositionColor(new Vector3(x, y, z), color)); // top-left
        //        rect.Add(new VertexPositionColor(new Vector3(x + w, y + h, z), color)); // bottom-right
        //        rect.Add(new VertexPositionColor(new Vector3(x + w, y, z), color)); // top-right
        //        var basicEffect = new BasicEffect(GraphicsDevice);
        //        basicEffect.VertexColorEnabled = true;
        //        basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1, 1000);
        //        var actualEffect = ((Game1)Game).blurHoriz;
        //        GraphicsDevice.SetRenderTarget(((Game1)Game).renderTarget2);
        //        //spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, actualEffect, null);
        //        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
        //SamplerState.LinearClamp, DepthStencilState.Default,
        //RasterizerState.CullNone, actualEffect);
        //        var actualRect = new Rectangle(x, y, w, h);
        //        //foreach (EffectPass pass in actualEffect.CurrentTechnique.Passes)
        //        {
        //            //GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, rect.ToArray());
        //            spriteBatch.Draw(((Game1)Game).renderTarget, actualRect, actualRect, color);
        //        }
        //        spriteBatch.End();
        //        GraphicsDevice.SetRenderTarget(null);
        //        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
        // SamplerState.LinearClamp, DepthStencilState.Default,
        // RasterizerState.CullNone, ((Game1)Game).blurVert);
        //        spriteBatch.Draw(((Game1)Game).renderTarget2, actualRect, actualRect, color);
        //        spriteBatch.End();
        //    }

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
            Vector2 measured = GlobalContent.Arial.MeasureString(text);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(GlobalContent.Arial, text, new Vector2(x + (w - measured.X) / 2, y + (h - measured.Y) / 2), Color.White);
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


// UI plan
// I think we still want to keep the original general shape
// the tab offset and roundedness will be configurable (it will be composed of beziers in the future)
// Probably not going to use any blur for this, just vertex colors

// maybe just go for a minimilistic glowing white line to start with
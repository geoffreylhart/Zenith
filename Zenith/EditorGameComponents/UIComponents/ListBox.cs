using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.Helpers;

namespace Zenith.EditorGameComponents.UIComponents
{
    class ListBox<T> : IUIComponent
    {
        private static int PADDING = 5;
        private static int LIST_ITEM_HEIGHT = 20;

        public int W { get; set; }
        public int H { get; set; }
        internal int activeIndex = 0;
        internal int hoverIndex = -1;
        private List<T> items;

        public ListBox(int w, List<T> items)
        {
            this.W = w;
            this.items = items;
        }

        public void Draw(GraphicsDevice graphicsDevice, int x, int y)
        {
            UITemp.DrawStyledBoxBack(x, y, W, (LIST_ITEM_HEIGHT + PADDING * 2) * items.Count, graphicsDevice, Game1.renderTarget);
            if (hoverIndex >= 0)
            {
                DrawBasicListItemBox(graphicsDevice, x, y, hoverIndex, new Color(0, 90, 128));
            }
            if (activeIndex >= 0)
            {
                DrawBasicListItemBox(graphicsDevice, x, y, activeIndex, new Color(0, 180, 255));
            }
            UITemp.DrawStyledBoxFront(x, y, W, (LIST_ITEM_HEIGHT + PADDING * 2) * items.Count, graphicsDevice, Game1.renderTarget);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin();
            for (int i = 0; i < items.Count; i++)
            {
                spriteBatch.DrawString(GlobalContent.ArialBold, GetItemAsString(items[i]), new Vector2(x + PADDING, y + PADDING + (LIST_ITEM_HEIGHT + PADDING * 2) * i), Color.White);
            }
            spriteBatch.End();
        }

        private void DrawBasicListItemBox(GraphicsDevice graphicsDevice, int x, int y, int index, Color color)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 1, 1000);
            float z = -10;
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            vertices.Add(new VertexPositionColor(new Vector3(x, y + (LIST_ITEM_HEIGHT + PADDING * 2) * (index + 1), z), color));
            vertices.Add(new VertexPositionColor(new Vector3(x, y + (LIST_ITEM_HEIGHT + PADDING * 2) * index, z), color));
            vertices.Add(new VertexPositionColor(new Vector3(x + W, y + (LIST_ITEM_HEIGHT + PADDING * 2) * (index + 1), z), color));
            vertices.Add(new VertexPositionColor(new Vector3(x + W, y + (LIST_ITEM_HEIGHT + PADDING * 2) * index, z), color));
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, vertices.ToArray());
            }
        }

        public void Update(int x, int y)
        {
            hoverIndex = -1;
            int mouseX = Mouse.GetState().X;
            int mouseY = Mouse.GetState().Y;
            if (mouseX >= x && mouseX <= x + W)
            {
                int temp = (mouseY - y) / (LIST_ITEM_HEIGHT + PADDING * 2);
                if (temp >= 0 && temp < items.Count)
                {
                    hoverIndex = temp;
                    if (UILayer.LeftPressed) activeIndex = temp;
                }
            }
            if (mouseX >= x && mouseX <= x + W && mouseY >= y && mouseY <= y + (LIST_ITEM_HEIGHT + PADDING * 2) * items.Count)
            {
                UILayer.ConsumeLeft();
            }
        }

        internal virtual string GetItemAsString(T item)
        {
            return item.ToString();
        }
    }
}

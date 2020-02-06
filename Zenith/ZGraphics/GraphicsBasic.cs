using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Helpers;

namespace Zenith.ZGraphics
{
    class GraphicsBasic
    {
        internal static void DrawScreenRect(GraphicsDevice graphicsDevice, double x, double y, double w, double h, Color color)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 1, 1000);
            DrawRect(graphicsDevice, basicEffect, x, y, w, h, color);
        }

        internal static void DrawScreenRect(GraphicsDevice graphicsDevice, double x, double y, double w, double h, Texture2D texture)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 1, 1000);
            DrawRect(graphicsDevice, basicEffect, x, y, w, h, texture);
        }

        internal static void DrawRect(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double x, double y, double w, double h, Color color)
        {
            float z = -10;
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            vertices.Add(new VertexPositionColor(new Vector3((float)x, (float)(y + h), z), color));
            vertices.Add(new VertexPositionColor(new Vector3((float)x, (float)y, z), color));
            vertices.Add(new VertexPositionColor(new Vector3((float)(x + w), (float)(y + h), z), color));
            vertices.Add(new VertexPositionColor(new Vector3((float)(x + w), (float)y, z), color));
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, vertices.ToArray());
            }
        }

        internal static void DrawRect(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double x, double y, double w, double h, Texture2D texture)
        {
            basicEffect.TextureEnabled = true;
            basicEffect.Texture = texture;
            float z = -10;
            List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
            // TODO: again, we're hacking to flip the image upside down, but we really should figure out the source of the issue
            vertices.Add(new VertexPositionTexture(new Vector3((float)x, (float)(y + h), z), new Vector2(0, 1)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)x, (float)y, z), new Vector2(0, 0)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)(x + w), (float)(y + h), z), new Vector2(1, 1)));
            vertices.Add(new VertexPositionTexture(new Vector3((float)(x + w), (float)y, z), new Vector2(1, 0)));
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, vertices.ToArray());
            }
        }

        internal static void DrawSpriteRectWeird(GraphicsDevice graphicsDevice, double x1, double y1, double w1, double h1, double x2, double y2, double w2, double h2, Texture2D texture)
        {
            Rectangle destRect = new Rectangle(0, 0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
            Rectangle srcRect = new Rectangle((int)(texture.Width * x2), (int)(texture.Height * y2), (int)(texture.Width * w2), (int)(texture.Height * h2));
            DoSpriteBatch(graphicsDevice, SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, texture, destRect, srcRect, Color.White);
        }

        internal static void DrawSpriteRect(GraphicsDevice graphicsDevice, int x, int y, int w, int h, Texture2D texture, BlendState blendState, Color color)
        {
            Rectangle destRect = new Rectangle(x, y, w, h);
            DoSpriteBatch(graphicsDevice, SpriteSortMode.Deferred, blendState, null, null, null, null, texture, destRect, color);
        }

        internal static void DrawSpriteRect(GraphicsDevice graphicsDevice, int x, int y, int w, int h, Texture2D texture, Effect effect, Color color)
        {
            Rectangle destRect = new Rectangle(x, y, w, h);
            DoSpriteBatch(graphicsDevice, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, effect, texture, destRect, color); // TODO: why immediate?
        }

        // does a simple spritebatch invocation, then reverts the graphicsDevice settings
        private static void DoSpriteBatch(GraphicsDevice graphicsDevice, SpriteSortMode spriteSortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Texture2D texture, Rectangle destRect, Color color)
        {
            var temp1 = graphicsDevice.BlendState;
            var temp2 = graphicsDevice.DepthStencilState;
            var temp3 = graphicsDevice.RasterizerState;
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(spriteSortMode, blendState, samplerState, depthStencilState, rasterizerState, effect);
            spriteBatch.Draw(texture, destRect, color);
            spriteBatch.End();
            graphicsDevice.BlendState = temp1;
            graphicsDevice.DepthStencilState = temp2;
            graphicsDevice.RasterizerState = temp3;
        }

        // does a simple spritebatch invocation, then reverts the graphicsDevice settings
        private static void DoSpriteBatch(GraphicsDevice graphicsDevice, SpriteSortMode spriteSortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Texture2D texture, Rectangle destRect, Rectangle srcRect, Color color)
        {
            var temp1 = graphicsDevice.BlendState;
            var temp2 = graphicsDevice.DepthStencilState;
            var temp3 = graphicsDevice.RasterizerState;
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(spriteSortMode, blendState, samplerState, depthStencilState, rasterizerState, effect);
            spriteBatch.Draw(texture, destRect, srcRect, color);
            spriteBatch.End();
            graphicsDevice.BlendState = temp1;
            graphicsDevice.DepthStencilState = temp2;
            graphicsDevice.RasterizerState = temp3;
        }
    }
}

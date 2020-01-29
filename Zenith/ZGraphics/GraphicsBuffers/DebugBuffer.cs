using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    public class DebugBuffer : IGraphicsBuffer
    {
        private SpriteBatch spriteBatch;
        private ISector sector;
        BasicVertexBuffer debugLinesBuffer;

        public DebugBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            this.sector = sector;
            this.spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void InitDraw(RenderContext context)
        {
        }

        public void Draw(RenderContext context)
        {
            if (context.layerPass != RenderContext.LayerPass.UI_PASS) return;
            if (debugLinesBuffer == null)
            {
                BlobCollection blobs = OSMReader.GetAllBlobs(sector);
                debugLinesBuffer = OSMLineBufferGenerator.GenerateDebugLines(context.graphicsDevice, blobs);
            }
            // draw those lines
            Effect effect = GlobalContent.DebugLinesShader;
            effect.Parameters["Texture"].SetValue(debugLinesBuffer.texture);
            effect.Parameters["WVP"].SetValue(context.WVP.toMatrix());
            effect.Parameters["ScreenSize"].SetValue(new Vector2(context.graphicsDevice.Viewport.Width, context.graphicsDevice.Viewport.Height));
            context.graphicsDevice.Indices = debugLinesBuffer.indices;
            context.graphicsDevice.SetVertexBuffer(debugLinesBuffer.vertices);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                context.graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, debugLinesBuffer.indices.IndexCount / 3);
            }
            // TODO: need to call this once for all debug buffers somehow
            string text = $"Nothing Selected";
            Vector2 size = GlobalContent.Arial.MeasureString(text);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(GlobalContent.Arial, text, new Vector2(context.graphicsDevice.Viewport.Width - 5 - size.X, 5), Color.White);
            spriteBatch.End();
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (debugLinesBuffer != null) debugLinesBuffer.Dispose();
            debugLinesBuffer = null;
        }
    }
}

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

        public void InitDraw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
        }

        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom, RenderTargetBinding[] targets)
        {
            if (targets != Game1.G_BUFFER && targets != Game1.RENDER_BUFFER) return;
            if (debugLinesBuffer == null)
            {
                BlobCollection blobs = OSMReader.GetAllBlobs(sector);
                debugLinesBuffer = OSMLineBufferGenerator.GenerateDebugLines(graphicsDevice, blobs);
            }
            // draw those lines
            Effect effect = GlobalContent.DebugLinesShader;
            graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            effect.Parameters["Texture"].SetValue(debugLinesBuffer.texture);
            effect.Parameters["WVP"].SetValue(basicEffect.World * basicEffect.View * basicEffect.Projection);
            effect.Parameters["ScreenSize"].SetValue(new Vector2(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height));
            graphicsDevice.Indices = debugLinesBuffer.indices;
            graphicsDevice.SetVertexBuffer(debugLinesBuffer.vertices);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, debugLinesBuffer.indices.IndexCount / 3);
            }
            // TODO: need to call this once for all debug buffers somehow
            string text = $"Nothing Selected";
            Vector2 size = GlobalContent.Arial.MeasureString(text);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(GlobalContent.Arial, text, new Vector2(graphicsDevice.Viewport.Width - 5 - size.X, 5), Color.White);
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

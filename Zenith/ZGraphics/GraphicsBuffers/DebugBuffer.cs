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
        BasicVertexBuffer roadsBuffer;

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
            if (roadsBuffer == null)
            {
                double widthInFeet = 10.7 * 50; // extra thick
                double circumEarth = 24901 * 5280;
                double width = widthInFeet / circumEarth * 2 * Math.PI;
                BlobCollection blobs = OSMReader.GetAllBlobs(sector);
                LineGraph roadGraph = blobs.GetRoadsFast();
                roadsBuffer = roadGraph.ConstructAsRoads(graphicsDevice, width * 4 / 50, GlobalContent.Error, Microsoft.Xna.Framework.Color.White);
            }
            roadsBuffer.Draw(graphicsDevice, basicEffect, targets);
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
            if (roadsBuffer != null) roadsBuffer.Dispose();
            roadsBuffer = null;
        }
    }
}

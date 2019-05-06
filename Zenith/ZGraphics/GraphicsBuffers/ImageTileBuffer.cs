using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.PrimitiveBuilder;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    // just an image of a sector
    class ImageTileBuffer : IGraphicsBuffer
    {
        VertexIndiceBuffer buffer;

        public ImageTileBuffer(GraphicsDevice graphicsDevice, Texture2D rendered, Sector sector)
        {
            buffer = SphereBuilder.MapMercatorToCylindrical(graphicsDevice, 2, Math.Pow(0.5, sector.zoom), sector.Latitude, sector.Longitude);
            buffer.texture = rendered;
        }

        public void Dispose()
        {
            buffer.Dispose();
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            BasicEffect basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);
            basicEffect.Texture = buffer.texture;
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.Indices = buffer.indices;
                graphicsDevice.SetVertexBuffer(buffer.vertices);
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
            }
            graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, graphicsDevice.Viewport.MaxDepth, 0);
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            return buffer.texture;
        }
    }
}

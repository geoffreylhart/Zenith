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
        internal static void DrawRect(GraphicsDevice graphicsDevice, float x, float y, float w, float h, Color color)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, 1, 1000);
            float z = -10;
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            vertices.Add(new VertexPositionColor(new Vector3(x, y + h, z), color));
            vertices.Add(new VertexPositionColor(new Vector3(x, y, z), color));
            vertices.Add(new VertexPositionColor(new Vector3(x + w, y + h, z), color));
            vertices.Add(new VertexPositionColor(new Vector3(x + w, y, z), color));
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, vertices.ToArray());
            }
        }
    }
}

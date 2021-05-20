using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.Drawables
{
    public class PlaneGrid : ZComponent
    {
        DynamicVertexIndexBuffer<VertexPositionColor> buffer = new DynamicVertexIndexBuffer<VertexPositionColor>();

        public PlaneGrid()
        {
            int count = 0;
            for (int i = -10; i <= 10; i++)
            {
                Color color = Color.White;
                if (i == 0) color = new Color(0, 0, 255); // z-axis
                buffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(Vector3.Right * i + Vector3.Forward * 10, color) });
                buffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(Vector3.Right * i + Vector3.Backward * 10, color) });
                if (i == 0) color = new Color(255, 0, 0); // x-axis
                buffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(Vector3.Forward * i + Vector3.Right * 10, color) });
                buffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(Vector3.Forward * i + Vector3.Left * 10, color) });
                buffer.AddIndices(new List<int>() { count * 4, count * 4 + 1, count * 4 + 2, count * 4 + 3 });
                count++;
            }
            // y-axis
            buffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(Vector3.Up * 10, new Color(0, 255, 0)) });
            buffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(Vector3.Down * 10, new Color(0, 255, 0)) });
            buffer.AddIndices(new List<int>() { count * 4, count * 4 + 1 });
        }

        public override void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            BasicEffect effect = new BasicEffect(graphics);
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
            effect.VertexColorEnabled = true;
            buffer.Draw(PrimitiveType.LineList, graphics, effect);
        }
    }
}

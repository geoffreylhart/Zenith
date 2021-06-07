using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.Drawables
{
    public class BoxOutline : ZComponent
    {
        private static DynamicVertexIndexBuffer<VertexPositionColor> boxBuffer;
        private static DynamicVertexIndexBuffer<VertexPositionColor> emptyBuffer;
        public BoundingBox boundingBox;
        public Color boxColor = Color.White;

        public BoxOutline(BoundingBox boundingBox)
        {
            this.boundingBox = boundingBox;
            if (boxBuffer == null)
            {
                boxBuffer = new DynamicVertexIndexBuffer<VertexPositionColor>();
                var axis = new[] { Vector3.Right, Vector3.Up, Vector3.Backward };
                for (int i = 0; i < 12; i++)
                {
                    var color = Color.White;
                    var mainAxis = axis[i % 3];
                    var otherAxis = axis.Where(x => x != mainAxis).ToArray();
                    var offset = Vector3.Zero;
                    if (i / 3 % 2 == 1) offset += otherAxis[0];
                    if (i / 3 / 2 == 1) offset += otherAxis[1];
                    boxBuffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(offset, color) });
                    boxBuffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(offset + mainAxis, color) });
                    boxBuffer.AddIndices(new List<int>() { i * 2, i * 2 + 1 });
                }
            }
            if (emptyBuffer == null)
            {
                emptyBuffer = new DynamicVertexIndexBuffer<VertexPositionColor>();
                var axis = new[] { Vector3.Right, Vector3.Up, Vector3.Backward };
                for (int i = 0; i < axis.Length; i++)
                {
                    var color = Color.White;
                    emptyBuffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(-axis[i], color) });
                    emptyBuffer.AddVertices(new List<VertexPositionColor>() { new VertexPositionColor(axis[i], color) });
                    emptyBuffer.AddIndices(new List<int>() { i * 2, i * 2 + 1 });
                }
            }
        }

        public override void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            if (boundingBox.Min == boundingBox.Max)
            {
                BasicEffect effect = new BasicEffect(graphics);
                effect.World = Matrix.CreateTranslation(boundingBox.Min) * world;
                effect.View = view;
                effect.Projection = projection;
                effect.VertexColorEnabled = true;
                effect.DiffuseColor = boxColor.ToVector3();
                emptyBuffer.Draw(PrimitiveType.LineList, graphics, effect);
            }
            else
            {
                BasicEffect effect = new BasicEffect(graphics);
                effect.World = Matrix.CreateScale(boundingBox.Max - boundingBox.Min) * Matrix.CreateTranslation(boundingBox.Min) * world;
                effect.View = view;
                effect.Projection = projection;
                effect.VertexColorEnabled = true;
                effect.DiffuseColor = boxColor.ToVector3();
                boxBuffer.Draw(PrimitiveType.LineList, graphics, effect);
            }
        }
    }
}

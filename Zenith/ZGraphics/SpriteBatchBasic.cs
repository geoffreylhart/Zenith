using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.ZGraphics
{
    class SpriteBatchBasic
    {
        static BlendState bmMask = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Add,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            BlendFactor = new Color(255, 255, 255, 255),
            ColorBlendFunction = BlendFunction.Add,
            ColorDestinationBlend = Blend.InverseSourceColor, // normally InverseSourceAlpha (opaque)
            ColorSourceBlend = Blend.Zero, // normally One (opaque)
            ColorWriteChannels = ColorWriteChannels.All,
            ColorWriteChannels1 = ColorWriteChannels.All,
            ColorWriteChannels2 = ColorWriteChannels.All,
            ColorWriteChannels3 = ColorWriteChannels.All,
            IndependentBlendEnable = false,
            MultiSampleMask = 2147483647
        };
        static BlendState bmMaskInvert = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Add,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            BlendFactor = new Color(255, 255, 255, 255),
            ColorBlendFunction = BlendFunction.Add,
            ColorDestinationBlend = Blend.SourceColor, // normally InverseSourceAlpha (opaque)
            ColorSourceBlend = Blend.Zero, // normally One (opaque)
            ColorWriteChannels = ColorWriteChannels.All,
            ColorWriteChannels1 = ColorWriteChannels.All,
            ColorWriteChannels2 = ColorWriteChannels.All,
            ColorWriteChannels3 = ColorWriteChannels.All,
            IndependentBlendEnable = false,
            MultiSampleMask = 2147483647
        };
        static BlendState additiveInvert = new BlendState()
        {
            AlphaBlendFunction = BlendFunction.Add,
            AlphaDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.SourceAlpha,
            BlendFactor = new Color(255, 255, 255, 255),
            ColorBlendFunction = BlendFunction.Add,
            ColorDestinationBlend = Blend.One,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorWriteChannels = ColorWriteChannels.All,
            ColorWriteChannels1 = ColorWriteChannels.All,
            ColorWriteChannels2 = ColorWriteChannels.All,
            ColorWriteChannels3 = ColorWriteChannels.All,
            IndependentBlendEnable = false,
            MultiSampleMask = 2147483647
        };

        internal static void DrawColorWithMask(GraphicsDevice graphicsDevice, int x, int y, int w, int h, Texture2D mask, Color color)
        {
            Effect tempEffect = GlobalContent.MaskShader.Clone();
            tempEffect.Parameters["maskColor"].SetValue(color.ToVector4());
            GraphicsBasic.DrawSpriteRect(graphicsDevice, x, y, w, h, mask, tempEffect, Color.White);
        }

        internal static void DrawColorWithInvertedMask(GraphicsDevice graphicsDevice, int x, int y, int w, int h, Texture2D mask, Color color, int lowest, int highest)
        {
            Effect tempEffect = GlobalContent.InvertedMaskShader.Clone();
            tempEffect.Parameters["maskColor"].SetValue(color.ToVector4());
            tempEffect.Parameters["lo"].SetValue(lowest / 255f);
            tempEffect.Parameters["hi"].SetValue(highest / 255f);
            GraphicsBasic.DrawSpriteRect(graphicsDevice, x, y, w, h, mask, tempEffect, Color.White);
        }
    }
}

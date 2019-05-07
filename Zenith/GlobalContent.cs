using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith
{
    class GlobalContent
    {
        public static Effect BlurShader;
        public static Effect MaskShader;
        public static Effect InvertedMaskShader;
        public static SpriteFont Arial;
        public static SpriteFont ArialBold;

        public static void Init(ContentManager content)
        {
            BlurShader = content.Load<Effect>("Shaders/BlurShader");
            MaskShader = content.Load<Effect>("Shaders/MaskShader");
            InvertedMaskShader = content.Load<Effect>("Shaders/InvertedMaskShader");
            Arial = content.Load<SpriteFont>("Fonts/Arial");
            ArialBold = content.Load<SpriteFont>("Fonts/ArialBold");
        }
    }
}

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
            BlurShader = content.Load<Effect>("BlurShader");
            MaskShader = content.Load<Effect>("MaskShader");
            InvertedMaskShader = content.Load<Effect>("InvertedMaskShader");
            Arial = content.Load<SpriteFont>("Arial");
            ArialBold = content.Load<SpriteFont>("ArialBold");
        }
    }
}

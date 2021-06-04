using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZManage
{
    class GlobalContent
    {
        public static Effect PointsShader;
        public static SpriteFont Arial;
        public static SpriteFont ArialBold;

        public static void Init(ContentManager content)
        {
            PointsShader = content.Load<Effect>("Shaders/PointsShader");
            Arial = content.Load<SpriteFont>("Fonts/Arial");
            ArialBold = content.Load<SpriteFont>("Fonts/ArialBold");
        }
    }
}

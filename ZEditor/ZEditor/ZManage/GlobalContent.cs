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

        public static void Init(ContentManager content)
        {
            PointsShader = content.Load<Effect>("Shaders/PointsShader");
        }
    }
}

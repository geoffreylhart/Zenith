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
        public static Effect TreeShader;
        public static SpriteFont Arial;
        public static SpriteFont ArialBold;
        public static Texture2D Road;
        public static Texture2D RoadTreeDensity;
        public static Texture2D Beach;
        public static Texture2D BeachTreeDensity;
        public static Texture2D BeachFlipped;
        public static Texture2D BeachFlippedTreeDensity;
        public static Texture2D CWArrows;
        public static Texture2D Error;
        public static Texture2D Grass;
        public static Texture2D GrassDepth;
        public static Texture2D Tree;
        public static Texture2D TreeDepth;

        public static void Init(ContentManager content)
        {
            BlurShader = content.Load<Effect>("Shaders/BlurShader");
            MaskShader = content.Load<Effect>("Shaders/MaskShader");
            InvertedMaskShader = content.Load<Effect>("Shaders/InvertedMaskShader");
            TreeShader = content.Load<Effect>("Shaders/TreeShader");
            Arial = content.Load<SpriteFont>("Fonts/Arial");
            ArialBold = content.Load<SpriteFont>("Fonts/ArialBold");
            Road = content.Load<Texture2D>("Images/Road");
            RoadTreeDensity = content.Load<Texture2D>("Images/RoadTreeDensity");
            Beach = content.Load<Texture2D>("Images/Beach");
            BeachTreeDensity = content.Load<Texture2D>("Images/BeachTreeDensity");
            BeachFlipped = content.Load<Texture2D>("Images/BeachFlipped");
            BeachFlippedTreeDensity = content.Load<Texture2D>("Images/BeachFlippedTreeDensity");
            CWArrows = content.Load<Texture2D>("Images/CWArrows");
            Error = content.Load<Texture2D>("Images/Error");
            Grass = content.Load<Texture2D>("Images/Grass");
            GrassDepth = content.Load<Texture2D>("Images/GrassDepth");
            Tree = content.Load<Texture2D>("Images/Tree");
            TreeDepth = content.Load<Texture2D>("Images/TreeDepth");
        }
    }
}

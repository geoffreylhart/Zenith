﻿using Microsoft.Xna.Framework.Content;
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
        public static Effect DeferredBasicShader;
        public static Effect DeferredLightingShader;
        public static Effect FXAAShader;
        public static Effect MaskShader;
        public static Effect InstancingShader;
        public static Effect InvertedMaskShader;
        public static Effect SSAOShader;
        public static Effect TreeShader;
        public static Effect TreeGeometryShader;
        public static Model House;
        public static Model StartingShuttle;
        public static SpriteFont Arial;
        public static SpriteFont ArialBold;
        public static Texture2D Beach;
        public static Texture2D BeachFlipped;
        public static Texture2D BeachFlippedTreeDensity;
        public static Texture2D BeachTreeDensity;
        public static Texture2D CWArrows;
        public static Texture2D Error;
        public static Texture2D Grass;
        public static Texture2D Road;
        public static Texture2D RoadTreeDensity;
        public static Texture2D Tree;

        public static void Init(ContentManager content)
        {
            BlurShader = content.Load<Effect>("Shaders/BlurShader");
            DeferredBasicShader = content.Load<Effect>("Shaders/DeferredBasicShader");
            DeferredLightingShader = content.Load<Effect>("Shaders/DeferredLightingShader");
            FXAAShader = content.Load<Effect>("Shaders/FXAAShader");
            MaskShader = content.Load<Effect>("Shaders/MaskShader");
            InstancingShader = content.Load<Effect>("Shaders/InstancingShader");
            InvertedMaskShader = content.Load<Effect>("Shaders/InvertedMaskShader");
            SSAOShader = content.Load<Effect>("Shaders/SSAOShader");
            TreeShader = content.Load<Effect>("Shaders/TreeShader");
            TreeGeometryShader = content.Load<Effect>("Shaders/TreeGeometryShader");
            House = content.Load<Model>("Models/House");
            StartingShuttle = content.Load<Model>("Models/StartingShuttle");
            Arial = content.Load<SpriteFont>("Fonts/Arial");
            ArialBold = content.Load<SpriteFont>("Fonts/ArialBold");
            Beach = content.Load<Texture2D>("Images/Beach");
            BeachFlipped = content.Load<Texture2D>("Images/BeachFlipped");
            BeachFlippedTreeDensity = content.Load<Texture2D>("Images/BeachFlippedTreeDensity");
            BeachTreeDensity = content.Load<Texture2D>("Images/BeachTreeDensity");
            CWArrows = content.Load<Texture2D>("Images/CWArrows");
            Error = content.Load<Texture2D>("Images/Error");
            Grass = content.Load<Texture2D>("Images/Grass");
            Road = content.Load<Texture2D>("Images/Road");
            RoadTreeDensity = content.Load<Texture2D>("Images/RoadTreeDensity");
            Tree = content.Load<Texture2D>("Images/Tree");
        }
    }
}

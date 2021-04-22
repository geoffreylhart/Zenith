using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates
{
    public interface ITemplate
    {
        void Load(StreamReader reader);
        void Save(StreamWriter writer);
        VertexIndexBuffer MakeFaceBuffer(GraphicsDevice graphics);
        VertexIndexBuffer MakeLineBuffer(GraphicsDevice graphics);
        VertexIndexBuffer MakePointBuffer(GraphicsDevice graphics);
        void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState, FPSCamera camera, GraphicsDevice graphicsDevice);
    }
}

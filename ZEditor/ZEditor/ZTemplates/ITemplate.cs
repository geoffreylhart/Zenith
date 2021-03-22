using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZGraphics;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates
{
    interface ITemplate
    {
        void Load(StreamReader reader);
        void Save(StreamWriter writer);
        VertexIndexBuffer MakeBuffer(GraphicsDevice graphics);
    }
}

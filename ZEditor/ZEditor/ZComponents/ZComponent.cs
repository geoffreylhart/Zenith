using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZControl;

namespace ZEditor.ZManage
{
    public class ZComponent
    {
        public virtual void Draw(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection) { }
        public virtual void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection) { }
        public virtual void Load(StreamReader reader, GraphicsDevice graphics) { }
        public virtual void Save(IndentableStreamWriter writer) { }
        public virtual void Update(UIContext uiContext) { }
    }
}

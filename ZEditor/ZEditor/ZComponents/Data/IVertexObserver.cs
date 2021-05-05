using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZComponents.Data
{
    public interface IVertexObserver
    {
        public void Add(int index, Vector3 v, Color color);
        public void Update(int index, Vector3 v, Color color);
    }
}

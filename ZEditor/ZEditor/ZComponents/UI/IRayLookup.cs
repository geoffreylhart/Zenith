using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZComponents.UI
{
    public interface IRayLookup<T>
    {
        T Get(Vector3 start, Vector3 look);
    }
}

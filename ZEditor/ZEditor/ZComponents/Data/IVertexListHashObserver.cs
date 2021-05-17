using System;
using System.Collections.Generic;
using System.Text;
using static ZEditor.ZComponents.Data.VertexDataComponent;

namespace ZEditor.ZComponents.Data
{
    public interface IVertexListHashObserver
    {
        public void Add(VertexData[] intList);
        public void Remove(VertexData[] intList);
    }
}

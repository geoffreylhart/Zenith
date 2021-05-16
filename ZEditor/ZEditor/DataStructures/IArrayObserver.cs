using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    public interface IArrayObserver<T>
    {
        public void WasAdded(T item);
        public void WasReduced(); // by one
        public void WasSet(int index, T item);
    }
}

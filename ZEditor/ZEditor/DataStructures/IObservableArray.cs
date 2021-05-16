using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    public interface IObservableArray<T>
    {
        public void AddObserver(IArrayObserver<T> observer);
        public int Count();
        public T Get(int index);
    }
}

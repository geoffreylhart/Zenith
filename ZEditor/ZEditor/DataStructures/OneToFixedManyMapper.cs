using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    class OneToFixedManyMapper<T1, T2> : IArrayObserver<T1>, IObservableArray<T2>
    {
        private IObservableArray<T1> observed;
        private Func<T1, T2[]> mapping;
        private int fixedSize = 0;
        private List<IArrayObserver<T2>> observers = new List<IArrayObserver<T2>>();

        public OneToFixedManyMapper(IObservableArray<T1> observed, Func<T1, T2[]> mapping)
        {
            this.observed = observed;
            observed.AddObserver(this);
            this.mapping = mapping;
        }

        public int Count()
        {
            return observed.Count() * fixedSize;
        }

        public void WasAdded(T1 item)
        {
            var newItems = mapping(item);
            fixedSize = newItems.Length;
            foreach (var newItem in newItems)
            {
                foreach (var observer in observers)
                {
                    observer.WasAdded(newItem);
                }
            }
        }

        public T2 Get(int index)
        {
            return mapping(observed.Get(index / fixedSize))[index % fixedSize];
        }

        public void WasReduced()
        {
            for (int i = 0; i < fixedSize; i++)
            {
                foreach (var observer in observers)
                {
                    observer.WasReduced();
                }
            }
        }

        public void WasSet(int index, T1 item)
        {
            var newItems = mapping(item);
            for (int i = 0; i < fixedSize; i++)
            {
                foreach (var observer in observers)
                {
                    observer.WasSet(index * fixedSize + i, newItems[i]);
                }
            }
        }

        public void AddObserver(IArrayObserver<T2> observer)
        {
            observers.Add(observer);
        }
    }
}

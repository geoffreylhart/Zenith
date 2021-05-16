using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    // TODO: maybe make these adapters AROUND IArrays or something, instead? then we won't need an observer pattern (especially since we don't need multiple watchers really?)
    // maps an array to variable amount in another array, sending out updates to observers
    // ordering is not guaranteed at all, even within individual mappings
    class OneToManyMapper<T1, T2> : IArrayObserver<T1>, IObservableArray<T2>
    {
        private Func<T1, T2[]> mapping;
        private List<ReverseLookup> data = new List<ReverseLookup>();
        private List<int[]> dataIndices = new List<int[]>();
        private List<IArrayObserver<T2>> observers = new List<IArrayObserver<T2>>();

        public OneToManyMapper(IObservableArray<T1> observed, Func<T1, T2[]> mapping)
        {
            observed.AddObserver(this);
            this.mapping = mapping;
        }

        public int Count()
        {
            return data.Count;
        }

        public void WasAdded(T1 item)
        {
            var newItems = mapping(item);
            var newItemIndices = new int[newItems.Length];
            dataIndices.Add(newItemIndices);
            for (int i = 0; i < newItems.Length; i++)
            {
                newItemIndices[i] = data.Count;
                data.Add(new ReverseLookup(newItems[i], dataIndices.Count - 1, i));
                foreach (var observer in observers)
                {
                    observer.WasAdded(newItems[i]);
                }
            }
        }

        public T2 Get(int index)
        {
            return data[index].item;
        }

        public void WasReduced()
        {
            var dataRemoving = dataIndices[dataIndices.Count - 1];
            dataIndices.RemoveAt(dataIndices.Count - 1);
            for (int i = 0; i < dataRemoving.Length; i++)
            {
                data[dataRemoving[i]] = data[data.Count - 1];
                dataIndices[data[data.Count - 1].t1Index][data[data.Count - 1].t1IndexIndex] = dataRemoving[i];
                foreach (var observer in observers)
                {
                    observer.WasSet(dataRemoving[i], data[data.Count - 1].item);
                }
                data.RemoveAt(data.Count - 1);
                foreach (var observer in observers)
                {
                    observer.WasReduced();
                }
            }
        }

        public void WasSet(int index, T1 item)
        {
            var oldIndices = dataIndices[index];
            var newItems = mapping(item);
            var newItemIndices = new int[newItems.Length];
            dataIndices[index] = newItemIndices;
            for (int i = 0; i < Math.Min(oldIndices.Length, newItems.Length); i++)
            {
                newItemIndices[i] = oldIndices[i];
                data[dataIndices[index][i]] = new ReverseLookup(newItems[i], index, i);
                foreach (var observer in observers)
                {
                    observer.WasSet(dataIndices[index][i], newItems[i]);
                }
            }
            if (newItems.Length > oldIndices.Length)
            {
                for (int i = oldIndices.Length; i < newItems.Length; i++)
                {
                    newItemIndices[i] = data.Count;
                    data.Add(new ReverseLookup(newItems[i], index, i));
                    foreach (var observer in observers)
                    {
                        observer.WasAdded(newItems[i]);
                    }
                }
            }
            if (oldIndices.Length > newItems.Length)
            {
                for (int i = newItems.Length; i < oldIndices.Length; i++)
                {
                    data[oldIndices[i]] = data[data.Count - 1];
                    dataIndices[data[data.Count - 1].t1Index][data[data.Count - 1].t1IndexIndex] = oldIndices[i];
                    foreach (var observer in observers)
                    {
                        observer.WasSet(oldIndices[i], data[data.Count - 1].item);
                    }
                    data.RemoveAt(data.Count - 1);
                    foreach (var observer in observers)
                    {
                        observer.WasReduced();
                    }
                }
            }
        }

        public void AddObserver(IArrayObserver<T2> observer)
        {
            observers.Add(observer);
        }

        private class ReverseLookup
        {
            public T2 item;
            public int t1Index;
            public int t1IndexIndex;

            public ReverseLookup(T2 item, int t1Index, int t1IndexIndex)
            {
                this.item = item;
                this.t1Index = t1Index;
                this.t1IndexIndex = t1IndexIndex;
            }
        }
    }
}

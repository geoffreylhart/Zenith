using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    // acts as hash while sending notifications on item indexing
    // ordering is not guaranteed at all
    public class IntListHashArray : IObservableArray<int[]>
    {
        private List<int[]> items = new List<int[]>();
        private Dictionary<int[], int> indices = new Dictionary<int[], int>();
        private List<IArrayObserver<int[]>> observers = new List<IArrayObserver<int[]>>();

        public int Count()
        {
            return items.Count;
        }

        public void Add(int[] item)
        {
            if (!indices.ContainsKey(item))
            {
                items.Add(item);
                indices[item] = items.Count - 1;
                foreach (var observer in observers) observer.WasAdded(item);
            }
        }

        public int[] Get(int index)
        {
            return items[index];
        }

        public void Remove(int[] item)
        {
            if (indices.ContainsKey(item))
            {
                int index = indices[item];
                indices.Remove(item);
                items[index] = items[items.Count - 1];
                foreach (var observer in observers)
                {
                    observer.WasSet(index, items[items.Count - 1]);
                }
                items.RemoveAt(items.Count - 1);
                foreach (var observer in observers)
                {
                    observer.WasReduced();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void AddObserver(IArrayObserver<int[]> observer)
        {
            observers.Add(observer);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    class ReversibleArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        public bool Equals(T[] x, T[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
            {
                if (!x[i].Equals(y[i]))
                {
                    for (int j = 0; j < x.Length; j++)
                    {
                        if (!x[j].Equals(y[x.Length - 1 - j])) return false;
                    }
                    return true;
                }
            }
            return true;
        }

        public int GetHashCode(T[] obj)
        {
            int result = 17;
            foreach (var x in obj) result = result * 23 + x.GetHashCode();
            int result2 = 17;
            for (int i = 0; i < obj.Length; i++) result2 = result2 * 23 + obj[obj.Length - 1 - i].GetHashCode();
            return Math.Min(result, result2);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    public class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        public bool Equals(T[] x, T[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
            {
                if (!x[i].Equals(y[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(T[] obj)
        {
            int result = 17;
            foreach (var x in obj) result = result * 23 + x.GetHashCode();
            return result;
        }
    }
}

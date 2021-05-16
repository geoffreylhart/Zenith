using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    class ReversibleIntListEqualityComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    for (int j = 0; j < x.Length; j++)
                    {
                        if (x[j] != y[x.Length - 1 - j]) return false;
                    }
                    return true;
                }
            }
            return true;
        }

        public int GetHashCode(int[] obj)
        {
            int result = 17;
            foreach (var x in obj) result = result * 23 + x;
            int result2 = 17;
            for (int i = 0; i < obj.Length; i++) result2 = result2 * 23 + obj[obj.Length - 1 - i];
            return Math.Min(result, result2);
        }
    }
}

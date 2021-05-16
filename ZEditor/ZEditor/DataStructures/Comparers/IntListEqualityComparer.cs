using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.DataStructures
{
    public class IntListEqualityComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(int[] obj)
        {
            int result = 17;
            foreach (var x in obj) result = result * 23 + x;
            return result;
        }
    }
}

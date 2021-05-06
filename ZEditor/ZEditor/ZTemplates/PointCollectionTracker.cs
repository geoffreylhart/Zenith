using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.UI;

namespace ZEditor.ZTemplates
{
    public class PointCollectionTracker : IVertexObserver, IRayLookup<int>
    {
        Dictionary<int, Vector3> dict = new Dictionary<int, Vector3>();

        public int Get(Vector3 start, Vector3 look)
        {
            double distance = double.MaxValue;
            int best = -1;
            foreach (var pair in dict)
            {
                double thisdistance = Distance(pair.Value, start, look);
                if (thisdistance < distance)
                {
                    distance = thisdistance;
                    best = pair.Key;
                }
            }
            return best;
        }

        public static double Distance(Vector3 n, Vector3 start, Vector3 look)
        {
            var perpendicular = Vector3.Cross(n - start, look);
            perpendicular.Normalize();
            perpendicular = Vector3.Cross(perpendicular, look);
            perpendicular.Normalize();
            return Math.Abs(Vector3.Dot(n - start, perpendicular));
        }

        public void Add(int index, Vector3 v, Color color)
        {
            dict[index] = v;
        }

        public void Update(int index, Vector3 v, Color color)
        {
            dict[index] = v;
        }
    }
}

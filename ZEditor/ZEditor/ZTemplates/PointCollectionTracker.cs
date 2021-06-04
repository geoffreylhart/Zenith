using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.UI;
using static ZEditor.ZComponents.Data.VertexDataComponent;

namespace ZEditor.ZTemplates
{
    public class PointCollectionTracker<T> : IRayLookup<T>
    {
        private IEnumerable<T> items;
        private Func<T, Vector3> positionFunction;

        public PointCollectionTracker(IEnumerable<T> items, Func<T, Vector3> positionFunction)
        {
            this.items = items;
            this.positionFunction = positionFunction;
        }

        public T Get(Vector3 start, Vector3 look)
        {
            double distance = double.MaxValue;
            T best = items.First();
            foreach (var item in items)
            {
                double thisdistance = Distance(positionFunction(item), start, look);
                if (thisdistance < distance)
                {
                    distance = thisdistance;
                    best = item;
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
    }
}

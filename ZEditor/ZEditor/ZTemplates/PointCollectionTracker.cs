using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.UI;
using static ZEditor.ZComponents.Data.VertexDataComponent;

namespace ZEditor.ZTemplates
{
    public class PointCollectionTracker : IRayLookup<VertexData>
    {
        public VertexDataComponent vertexData = new VertexDataComponent();

        public VertexData Get(Vector3 start, Vector3 look)
        {
            double distance = double.MaxValue;
            VertexData best = null;
            foreach (var v in vertexData.vertexData)
            {
                double thisdistance = Distance(v.position, start, look);
                if (thisdistance < distance)
                {
                    distance = thisdistance;
                    best = v;
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

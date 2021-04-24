﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZTemplates
{
    public class PointCollectionTracker
    {
        Dictionary<int, Vector3> dict = new Dictionary<int, Vector3>();

        internal void Track(int index, Vector3 v)
        {
            dict[index] = v;
        }

        internal int GetNearest(Vector3 start, Vector3 look)
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

        internal void Update(int nearestIndice, Vector3 v)
        {
            dict[nearestIndice] = v;
        }
    }
}
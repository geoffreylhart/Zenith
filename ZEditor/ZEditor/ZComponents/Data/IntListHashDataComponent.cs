using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.Data
{
    class IntListHashDataComponent : ZComponent, IIntListHashObserver
    {
        public HashSet<int[]> intLists = new HashSet<int[]>(new IntListEqualityComparer()); // TODO: make readonly
        private List<IIntListHashObserver> observers = new List<IIntListHashObserver>();

        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            var currLine = reader.ReadLine();
            if (!currLine.Contains("Quads")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                Add(currLine.Trim().Split(',').Select(x => int.Parse(x)).ToArray());
                currLine = reader.ReadLine();
            }
            currLine = reader.ReadLine();
            if (!currLine.Contains("Tris")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                Add(currLine.Trim().Split(',').Select(x => int.Parse(x)).ToArray());
                currLine = reader.ReadLine();
            }
        }

        public override void Save(StreamWriter writer)
        {
            throw new NotImplementedException();
        }

        internal void AddObserver(IIntListHashObserver observer)
        {
            observers.Add(observer);
        }

        public void Add(int[] intList)
        {
            if (!intLists.Contains(intList))
            {
                intLists.Add(intList);
                foreach (var observer in observers) observer.Add(intList);
            }
        }

        private class IntListEqualityComparer : IEqualityComparer<int[]>
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
}

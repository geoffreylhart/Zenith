using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.DataStructures;
using ZEditor.ZManage;
using static ZEditor.ZComponents.Data.VertexDataComponent;

namespace ZEditor.ZComponents.Data
{
    class VertexListHashDataComponent : ZComponent, IVertexListHashObserver
    {
        public VertexDataComponent vertexData;
        public HashSet<VertexData[]> lists = new HashSet<VertexData[]>(new ArrayEqualityComparer<VertexData>());
        private List<IVertexListHashObserver> observers = new List<IVertexListHashObserver>();

        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            var currLine = reader.ReadLine();
            if (!currLine.Contains("Quads")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                Add(currLine.Trim().Split(',').Select(x => vertexData[int.Parse(x)]).ToArray());
                currLine = reader.ReadLine();
            }
            currLine = reader.ReadLine();
            if (!currLine.Contains("Tris")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                Add(currLine.Trim().Split(',').Select(x => vertexData[int.Parse(x)]).ToArray());
                currLine = reader.ReadLine();
            }
        }

        public override void Save(IndentableStreamWriter writer)
        {
            writer.WriteLine("Quads {");
            writer.Indent();
            foreach (var intList in lists)
            {
                if (intList.Length == 4)
                {
                    writer.WriteLine(string.Join(",", intList.Select(x => vertexData.IndexOf(x))));
                }
            }
            writer.UnIndent();
            writer.WriteLine("}");
            writer.WriteLine("Tris {");
            writer.Indent();
            foreach (var intList in lists)
            {
                if (intList.Length == 3)
                {
                    writer.WriteLine(string.Join(",", intList.Select(x => vertexData.IndexOf(x))));
                }
            }
            writer.UnIndent();
            writer.WriteLine("}");
        }

        internal void AddObserver(IVertexListHashObserver observer)
        {
            observers.Add(observer);
        }

        public void Add(VertexData[] intList)
        {
            if (!lists.Contains(intList))
            {
                lists.Add(intList);
                foreach (var observer in observers) observer.Add(intList);
            }
        }

        public void Remove(VertexData[] intList)
        {
            if (lists.Contains(intList))
            {
                lists.Remove(intList);
                foreach (var observer in observers) observer.Remove(intList);
            }
        }
    }
}

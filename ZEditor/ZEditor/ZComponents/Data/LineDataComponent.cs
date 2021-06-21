using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZManage;
using static ZEditor.ZComponents.Data.LineDataComponent;
using static ZEditor.ZComponents.Data.VertexDataComponent;

namespace ZEditor.ZComponents.Data
{
    public class LineDataComponent : ZComponent
    {
        public VertexDataComponent vertexData;
        private HashSet<LineData> lineData = new HashSet<LineData>();

        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            var currLine = reader.ReadLine();
            if (!currLine.Contains("Lines")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',').Select(x => vertexData[int.Parse(x)]).ToArray();
                Add(new LineData(split[0], split[1]));
                currLine = reader.ReadLine();
            }
        }

        public override void Save(IndentableStreamWriter writer)
        {
            writer.WriteLine("Lines {");
            writer.Indent();
            foreach (var line in lineData)
            {
                writer.WriteLine(vertexData.IndexOf(line.v1) + "," + vertexData.IndexOf(line.v2));
            }
            writer.UnIndent();
            writer.WriteLine("}");
        }

        public void Add(LineData item)
        {
            lineData.Add(item);
        }

        public void Remove(LineData item)
        {
            lineData.Remove(item);
        }

        public class LineData
        {
            public VertexData v1;
            public VertexData v2;
            public LineData(VertexData v1, VertexData v2)
            {
                this.v1 = v1;
                this.v2 = v2;
            }

            public override bool Equals(object obj)
            {
                LineData that = (LineData)obj;
                if (this.v1 == that.v1 && this.v2 == that.v2) return true;
                if (this.v1 == that.v2 && this.v2 == that.v1) return true;
                return false;
            }

            public override int GetHashCode()
            {
                int item1 = v1.GetHashCode();
                int item2 = v2.GetHashCode();
                if (item1 > item2)
                {
                    int temp = item1;
                    item1 = item2;
                    item2 = temp;
                }
                return (17 * 23 + item1) * 23 + item2;
            }
        }
    }
}

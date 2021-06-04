using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZManage;
using ZEditor.ZTemplates;
using ZEditor.ZTemplates.Mesh;
using static ZEditor.ZComponents.Data.VertexDataComponent;

namespace ZEditor.ZComponents.Data
{
    public class VertexDataComponent : ZComponent, IEnumerable<VertexData>
    {
        public bool saveColor;
        private List<VertexData> vertexData = new List<VertexData>();

        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            var currLine = reader.ReadLine();
            if (!currLine.Contains("Vertices")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                vertexData.Add(new VertexData(new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2])), Color.Black));
                currLine = reader.ReadLine();
            }
        }

        public override void Save(IndentableStreamWriter writer)
        {
            writer.WriteLine("Vertices {");
            writer.Indent();
            foreach (var vertex in vertexData)
            {
                writer.WriteLine(vertex.position.X + "," + vertex.position.Y + "," + vertex.position.Z);
            }
            writer.UnIndent();
            writer.WriteLine("}");
        }

        public IEnumerator<VertexData> GetEnumerator()
        {
            return vertexData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return vertexData.GetEnumerator();
        }

        public int IndexOf(VertexData x)
        {
            return vertexData.IndexOf(x);
        }

        public VertexData this[int i]
        {
            get { return vertexData[i]; }
            set { vertexData[i] = value; }
        }

        public void Add(VertexData item)
        {
            vertexData.Add(item);
        }

        public void AddRange(IEnumerable<VertexData> collection)
        {
            vertexData.AddRange(collection);
        }

        public class VertexData
        {
            public Vector3 position;
            public Color color;
            public VertexData(Vector3 position, Color color)
            {
                this.position = position;
                this.color = color;
            }
        }
    }
}

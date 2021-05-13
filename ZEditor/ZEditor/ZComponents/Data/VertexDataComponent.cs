using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZManage;
using ZEditor.ZTemplates;
using ZEditor.ZTemplates.Mesh;

namespace ZEditor.ZComponents.Data
{
    public class VertexDataComponent : ZComponent, IVertexObserver
    {
        public bool saveColor;
        public List<Vector3> positions = new List<Vector3>(); // TODO: make readonly
        public List<Color> colors = new List<Color>();
        private List<IVertexObserver> observers = new List<IVertexObserver>();

        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            var currLine = reader.ReadLine();
            if (!currLine.Contains("Vertices")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(',');
                Add(positions.Count, new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2])), Color.Black);
                currLine = reader.ReadLine();
            }
        }

        public override void Save(IndentableStreamWriter writer)
        {
            writer.WriteLine("Vertices {");
            writer.Indent();
            foreach (var position in positions)
            {
                writer.WriteLine(position.X + "," + position.Y + "," + position.Z);
            }
            writer.UnIndent();
            writer.WriteLine("}");
        }

        internal void AddObserver(IVertexObserver observer)
        {
            observers.Add(observer);
        }

        public void Add(int index, Vector3 v, Color color)
        {
            positions.Add(v);
            colors.Add(color);
            foreach (var observer in observers) observer.Add(positions.Count - 1, v, color);
        }

        public void Update(int index, Vector3 v, Color color)
        {
            positions[index] = v;
            colors[index] = color;
            foreach (var observer in observers) observer.Update(index, v, color);
        }
    }
}

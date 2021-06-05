using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZManage;
using static ZEditor.ZComponents.Data.ReferenceDataComponent;

namespace ZEditor.ZComponents.Data
{
    public class ReferenceDataComponent : ZComponent, IEnumerable<Reference>
    {
        private List<Reference> references = new List<Reference>();

        public override void Load(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            base.Load(reader, graphicsDevice);
            var currLine = reader.ReadLine();
            if (!currLine.Contains("References")) throw new NotImplementedException();
            currLine = reader.ReadLine();
            while (!currLine.Contains("}"))
            {
                var split = currLine.Trim().Split(' ');
                var refName = split[0].Trim('"');
                var posSplit = split[1].Split(',');
                references.Add(new Reference(refName, new Vector3(float.Parse(posSplit[0]), float.Parse(posSplit[1]), float.Parse(posSplit[2]))));
                currLine = reader.ReadLine();
            }
        }

        public override void Save(IndentableStreamWriter writer)
        {
            base.Save(writer);
            writer.WriteLine("References {");
            writer.Indent();
            foreach (var reference in references)
            {
                writer.WriteLine("\"" + reference.name + "\" " + reference.position.X + "," + reference.position.Y + "," + reference.position.Z);
            }
            writer.UnIndent();
            writer.WriteLine("}");
        }

        public IEnumerator<Reference> GetEnumerator()
        {
            return references.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return references.GetEnumerator();
        }

        public int IndexOf(Reference x)
        {
            return references.IndexOf(x);
        }

        public Reference this[int i]
        {
            get { return references[i]; }
            set { references[i] = value; }
        }

        public void Add(Reference item)
        {
            references.Add(item);
        }

        public void Remove(Reference item)
        {
            references.Remove(item);
        }

        public void AddRange(IEnumerable<Reference> collection)
        {
            references.AddRange(collection);
        }

        public class Reference
        {
            public string name;
            public Vector3 position;
            public Reference(string name, Vector3 position)
            {
                this.name = name;
                this.position = position;
            }
        }
    }
}

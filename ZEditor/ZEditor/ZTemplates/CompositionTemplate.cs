using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZManage;

namespace ZEditor.ZTemplates
{
    // potentially this class will house not just meshes, but basically all node flow between modular things
    public class CompositionTemplate : ZGameObject
    {
        private List<Reference> references = new List<Reference>();
        public CompositionTemplate()
        {

        }
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
        public override void Draw(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            base.Draw(graphics, world, view, projection);
            foreach(var reference in references)
            {
                var obj = TemplateManager.LOADED_TEMPLATES[reference.name];
                obj.Draw(graphics, world, view, projection);
            }
        }

        private class Reference
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

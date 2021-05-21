using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZEditor.ZComponents.Data;
using ZEditor.ZGraphics;
using ZEditor.ZTemplates;

namespace ZEditor.ZManage
{
    public class TemplateManager
    {
        private static Type[] TEMPLATE_TYPES = new Type[] { typeof(MeshTemplate), typeof(CompositionTemplate) };
        public static Dictionary<string, ZGameObject> LOADED_TEMPLATES = new Dictionary<string, ZGameObject>();

        public static ZGameObject Load(string fileName, string templateName, GraphicsDevice graphicsDevice)
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            string fullPath = Path.Combine(rootDirectory.Substring(0, rootDirectory.IndexOf("ZEditor")), "ZEditor\\ZEditor", fileName);
            using (var reader = new StreamReader(fullPath))
            {
                while (!reader.EndOfStream)
                {
                    var match = Regex.Match(reader.ReadLine(), "^\"([^\"]*)\" {$");
                    if (match.Success)
                    {
                        string name = match.Groups[1].Value;
                        LOADED_TEMPLATES[name] = ReadTemplate(reader, graphicsDevice);
                    }
                }
            }
            return LOADED_TEMPLATES[templateName];
        }

        internal static void Save(string fileName)
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            string fullPath = Path.Combine(rootDirectory.Substring(0, rootDirectory.IndexOf("ZEditor")), "ZEditor\\ZEditor", fileName);
            using (var writer = new IndentableStreamWriter(fullPath))
            {
                foreach (var templatePair in LOADED_TEMPLATES)
                {
                    writer.WriteLine("\"" + templatePair.Key + "\" {");
                    writer.Indent();
                    writer.WriteLine(templatePair.Value.GetType().Name + " {");
                    writer.Indent();
                    templatePair.Value.Save(writer);
                    writer.UnIndent();
                    writer.WriteLine("}");
                    writer.UnIndent();
                    writer.WriteLine("}");
                }
            }
        }

        private static ZGameObject ReadTemplate(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            string nameLine = reader.ReadLine();
            string thisName = Regex.Match(nameLine, "^ *([^ ]+) {$").Groups[1].Value;
            foreach (var templateType in TEMPLATE_TYPES)
            {
                if (thisName == templateType.Name)
                {
                    ZGameObject template = (ZGameObject)templateType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                    template.Load(reader, graphicsDevice);
                    return template;
                }
            }
            throw new NotImplementedException();
        }
    }
}

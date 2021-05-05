using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZEditor.ZGraphics;
using ZEditor.ZTemplates;

namespace ZEditor.ZManage
{
    public class TemplateManager
    {
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
                        if (name == templateName)
                        {
                            return ReadTemplate(reader, graphicsDevice);
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private static ZGameObject ReadTemplate(StreamReader reader, GraphicsDevice graphicsDevice)
        {
            string nameLine = reader.ReadLine();
            string thisName = Regex.Match(nameLine, "^ *([^ ]+) {$").Groups[1].Value;
            switch (thisName)
            {
                case "Mesh":
                    var mesh = new MeshTemplate();
                    mesh.Load(reader, graphicsDevice);
                    return mesh;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

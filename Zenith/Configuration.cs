using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Zenith.LibraryWrappers.OSM;

namespace Zenith
{
    internal class Configuration
    {
        private static String FILE_PATH = OSMPaths.GetLocalCacheRoot() + @"\Settings.ini";
        // TODO: Why isn't reflection working with internal/private? I thought it was supposed to be able to
        public static bool AUTO_LOAD = false;

        internal static void Save()
        {
            List<String> lines = new List<string>();
            foreach (var field in typeof(Configuration).GetFields())
            {
                if (!field.IsPrivate && field.IsStatic)
                {
                    lines.Add(field.Name + "=" + field.GetValue(null));
                }
            }
            File.WriteAllLines(FILE_PATH, lines);
        }

        internal static void Load()
        {
            if (!File.Exists(FILE_PATH)) return;
            String[] lines = File.ReadAllLines(FILE_PATH);
            foreach (var line in lines)
            {
                String[] split = line.Split('=');
                String name = split[0];
                String value = split[1];
                var field = typeof(Configuration).GetField(name);
                Object valueCast = null;
                if (field.FieldType == typeof(String))
                {
                    valueCast = value;
                }
                if (field.FieldType == typeof(bool))
                {
                    valueCast = bool.Parse(value);
                }
                if (field.FieldType == typeof(int))
                {
                    valueCast = int.Parse(value);
                }
                field.SetValue(null, valueCast);
            }
        }
    }
}

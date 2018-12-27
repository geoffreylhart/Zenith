using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class OpenStreetMaps : SectorLoader
    {
        private static string mapFolder = @"..\..\..\..\LocalCache\OpenStreetMaps\";

        public override bool CacheExists(Sector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            String filePath = mapFolder + fileName;
            return File.Exists(filePath);
        }

        public override IEnumerable<Sector> EnumerateCachedSectors()
        {
            foreach (var file in Directory.EnumerateFiles(mapFolder))
            {
                String filename = Path.GetFileName(file);
                if (filename.StartsWith("X"))
                {
                    String[] split = filename.Split(',');
                    int x = int.Parse(split[0].Split('=')[1]);
                    int y = int.Parse(split[1].Split('=')[1]);
                    int zoom = int.Parse(split[2].Split('=', '.')[1]);
                    yield return new Sector(x, y, zoom);
                }
            }
        }

        public override Texture2D GetTexture(GraphicsDevice graphicsDevice, Sector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            // check for composite first
            if (File.Exists(mapFolder + fileName))
            {
                using (var reader = File.OpenRead(mapFolder + fileName))
                {
                    return Texture2D.FromStream(graphicsDevice, reader);
                }
            }
            // otherwise, build it
            return OpenStreetMap.GetRoads(graphicsDevice, sector);
        }
    }
}

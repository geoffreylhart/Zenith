using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class GoogleMaps : SectorLoader
    {
        public override bool CacheExists(Sector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            String filePath = @"..\..\..\..\LocalCache\GoogleMaps\" + fileName;
            return File.Exists(filePath);
        }

        public override IEnumerable<Sector> EnumerateCachedSectors()
        {
            foreach (var file in Directory.EnumerateFiles(@"..\..\..\..\LocalCache\GoogleMaps"))
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
            String filePath = @"..\..\..\..\LocalCache\GoogleMaps\" + fileName;
            if (File.Exists(filePath))
            {
                using (var reader = File.OpenRead(filePath))
                {
                    return Texture2D.FromStream(graphicsDevice, reader);
                }
            }
            else
            {
                Texture2D texture = MapGenerator.GetMap(graphicsDevice, sector.Longitude * 180 / Math.PI, sector.Latitude * 180 / Math.PI, sector.zoom);
                using (var writer = File.OpenWrite(filePath))
                {
                    texture.SaveAsPng(writer, texture.Width, texture.Height);
                }
                return texture;
            }
        }
    }
}

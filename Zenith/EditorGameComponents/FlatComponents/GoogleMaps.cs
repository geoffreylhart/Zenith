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
            String filePath = @"..\..\..\..\LocalCache\GoogleMaps\Composite\" + fileName;
            return File.Exists(filePath);
        }

        public override IEnumerable<Sector> EnumerateCachedSectors()
        {
            foreach (var file in Directory.EnumerateFiles(@"..\..\..\..\LocalCache\GoogleMaps\Composite"))
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
            if (File.Exists(@"..\..\..\..\LocalCache\GoogleMaps\Composite\" + fileName))
            {
                using (var reader = File.OpenRead(@"..\..\..\..\LocalCache\GoogleMaps\Composite\" + fileName))
                {
                    return Texture2D.FromStream(graphicsDevice, reader);
                }
            }
            // otherwise, build it
            return GetMap(graphicsDevice, sector, MapGenerator.MapStyle.TERRAIN, false);
            //Texture2D satellite = GetMap(graphicsDevice, sector, MapGenerator.MapStyle.SATELLITE, true);
            //Texture2D terrain = GetMap(graphicsDevice, sector, MapGenerator.MapStyle.TERRAIN, true);
            //Texture2D composite = MakeComposite(satellite, terrain);
            //using (var writer = File.OpenWrite(@"..\..\..\..\LocalCache\GoogleMaps\Composite\" + fileName))
            //{
            //    composite.SaveAsPng(writer, composite.Width, composite.Height);
            //}
            //return composite;
        }

        private Texture2D MakeComposite(Texture2D satellite, Texture2D terrain)
        {
            return satellite;
        }

        private Texture2D GetMap(GraphicsDevice graphicsDevice, Sector sector, MapGenerator.MapStyle mapStyle, bool save)
        {
            String fileName = sector.ToString() + ".PNG";
            String styleStr = mapStyle.ToString().Substring(0, 1).ToUpper() + mapStyle.ToString().Substring(1).ToLower();
            if (File.Exists(@"..\..\..\..\LocalCache\GoogleMaps\" + styleStr + "\\" + fileName))
            {
                using (var reader = File.OpenRead(@"..\..\..\..\LocalCache\GoogleMaps\" + styleStr + "\\" + fileName))
                {
                    return Texture2D.FromStream(graphicsDevice, reader);
                }
            }
            else
            {
                Texture2D texture = MapGenerator.GetMap(graphicsDevice, sector, mapStyle);
                if (save)
                {
                    using (var writer = File.OpenWrite(@"..\..\..\..\LocalCache\GoogleMaps\" + styleStr + "\\" + fileName))
                    {
                        texture.SaveAsPng(writer, texture.Width, texture.Height);
                    }
                }
                return texture;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers;
using Zenith.ZGraphics;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class OpenStreetMaps : SectorLoader
    {
        private static string mapFolder = @"..\..\..\..\LocalCache\OpenStreetMaps\Renders\";

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
            // TODO: partial picture saving of textures is happening for some other reason - not internet connection issues
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
            Texture2D rendered;
            if (sector.zoom >= 10)
            {
                rendered = OpenStreetMap.GetRoads(graphicsDevice, sector);
            }
            else
            {
                rendered = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
                List<Sector> roadSectors = sector.GetChildrenAtLevel(sector.zoom + 1);
                Texture2D[] textures = new Texture2D[roadSectors.Count];
                for (int i = 0; i < roadSectors.Count; i++)
                {
                    textures[i] = GetTexture(graphicsDevice, roadSectors[i]);
                }
                if (textures.Any(x => x != null))
                {
                    graphicsDevice.SetRenderTarget((RenderTarget2D)rendered);
                    for (int i = 0; i < roadSectors.Count; i++)
                    {
                        int size, x, y;
                        int subd = 1 << (roadSectors[i].zoom - sector.zoom);
                        size = 512 >> (roadSectors[i].zoom - sector.zoom);
                        x = sector.GetRelativeXOf(roadSectors[i]) * size;
                        y = (subd - 1 - sector.GetRelativeYOf(roadSectors[i])) * size;
                        if (textures[i] != null)
                        {
                            GraphicsBasic.DrawSpriteRect(graphicsDevice, x, y, size, size, textures[i], BlendState.AlphaBlend, Color.White);
                        }
                    }
                }
            }
            // save texture
            using (var writer = File.OpenWrite(mapFolder + fileName))
            {
                rendered.SaveAsPng(writer, rendered.Width, rendered.Height);
            }
            return rendered;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGraphics;
using Zenith.ZGraphics.GraphicsBuffers;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class OSMSectorLoader : SectorLoader
    {
        private static string mapFolder = @"..\..\..\..\LocalCache\OpenStreetMaps\Renders\";

        public override bool CacheExists(MercatorSector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            String filePath = mapFolder + fileName;
            return File.Exists(filePath);
        }

        public override bool DoAutoLoad(MercatorSector sector)
        {
            return sector.Zoom <= 7 || sector.Zoom == 10;
        }

        public override bool AllowUnload(MercatorSector sector)
        {
            return sector.Zoom <= 7;
        }

        public override IEnumerable<MercatorSector> EnumerateCachedSectors()
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
                    yield return new MercatorSector(x, y, zoom);
                }
            }
        }

        public override IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            if (File.Exists(mapFolder + fileName))
            {
                using (var reader = File.OpenRead(mapFolder + fileName))
                {
                    return new ImageTileBuffer(graphicsDevice, Texture2D.FromStream(graphicsDevice, reader), sector);
                }
            }
            // otherwise, build it
            if (sector.Zoom >= 10)
            {
                VectorTileBuffer buffer = new VectorTileBuffer();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                BlobCollection blobs = OSMReader.GetAllBlobs(sector);
                Console.WriteLine($"{sector} blobs loaded in {sw.Elapsed.TotalSeconds} seconds.");
                sw.Restart();
                buffer.Add(graphicsDevice, OSMBufferGenerator.GetCoast(graphicsDevice, blobs, sector), sector);
                buffer.Add(graphicsDevice, OSMBufferGenerator.GetCoastBorder(graphicsDevice, blobs), sector);
                Console.WriteLine($"{sector} coast loaded in {sw.Elapsed.TotalSeconds} seconds.");
                sw.Restart();
                buffer.Add(graphicsDevice, OSMBufferGenerator.GetLakes(graphicsDevice, blobs, sector), sector);
                //buffer.Add(graphicsDevice, OSMBufferGenerator.GetLakesBorder(graphicsDevice, blobs, sector), sector);
                Console.WriteLine($"{sector} lakes loaded in {sw.Elapsed.TotalSeconds} seconds.");
                sw.Restart();
                buffer.Add(graphicsDevice, OSMBufferGenerator.GetRoads(graphicsDevice, blobs), sector);
                Console.WriteLine($"{sector} roads loaded in {sw.Elapsed.TotalSeconds} seconds.");
                sw.Restart();
                //buffer.Add(graphicsDevice, OSMBufferGenerator.GetTrees(graphicsDevice, blobs, sector), sector);
                Console.WriteLine($"{sector} trees loaded in {sw.Elapsed.TotalSeconds} seconds.");
                return buffer;
            }
            else
            {
                // combination image
                Texture2D rendered = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
                List<ISector> roadSectors = sector.GetChildrenAtLevel(sector.Zoom + 1);
                Texture2D[] textures = new Texture2D[roadSectors.Count];
                for (int i = 0; i < roadSectors.Count; i++)
                {
                    textures[i] = GetGraphicsBuffer(graphicsDevice, roadSectors[i]).GetImage(graphicsDevice);
                }
                if (textures.Any(x => x != null))
                {
                    graphicsDevice.SetRenderTarget((RenderTarget2D)rendered);
                    for (int i = 0; i < roadSectors.Count; i++)
                    {
                        int size, x, y;
                        int subd = 1 << (roadSectors[i].Zoom - sector.Zoom);
                        size = 512 >> (roadSectors[i].Zoom - sector.Zoom);
                        x = sector.GetRelativeXOf(roadSectors[i]) * size;
                        y = (subd - 1 - sector.GetRelativeYOf(roadSectors[i])) * size;
                        if (textures[i] != null)
                        {
                            GraphicsBasic.DrawSpriteRect(graphicsDevice, x, y, size, size, textures[i], BlendState.AlphaBlend, Color.White);
                        }
                    }
                }
                for (int i = 0; i < textures.Length; i++)
                {
                    if (textures[i] != null) textures[i].Dispose();
                }
                if (sector.Zoom <= 7)
                {
                    using (var writer = File.OpenWrite(mapFolder + fileName))
                    {
                        rendered.SaveAsPng(writer, rendered.Width, rendered.Height);
                    }
                }
                return new ImageTileBuffer(graphicsDevice, rendered, sector);
            }
        }
    }
}

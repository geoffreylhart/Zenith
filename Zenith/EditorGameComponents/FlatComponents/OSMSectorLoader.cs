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
using Zenith.ZGeom;
using Zenith.ZGraphics;
using Zenith.ZGraphics.GraphicsBuffers;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class OSMSectorLoader : SectorLoader
    {
        public override bool CacheExists(ISector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            String filePath = Path.Combine(OSMPaths.GetRenderRoot(), fileName);
            return File.Exists(filePath);
        }

        public override bool DoAutoLoad(ISector sector)
        {
            return sector.Zoom == ZCoords.GetSectorManager().GetHighestOSMZoom();
        }

        public override bool AllowUnload(ISector sector)
        {
            return sector.Zoom <= ZCoords.GetSectorManager().GetHighestOSMZoom() - 3;
        }

        public override IEnumerable<ISector> EnumerateCachedSectors()
        {
            var manager = ZCoords.GetSectorManager();
            foreach (var file in Directory.EnumerateFiles(OSMPaths.GetRenderRoot()))
            {
                String filename = Path.GetFileName(file);
                if (!filename.StartsWith("Coast"))
                {
                    yield return manager.FromString(filename.Split('.')[0]);
                }
            }
        }

        public override IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            if (File.Exists(Path.Combine(OSMPaths.GetRenderRoot(), fileName)))
            {
                using (var reader = File.OpenRead(Path.Combine(OSMPaths.GetRenderRoot(), fileName)))
                {
                    return new ImageTileBuffer(graphicsDevice, Texture2D.FromStream(graphicsDevice, reader), sector);
                }
            }
            // otherwise, build it
            if (sector.Zoom >= ZCoords.GetSectorManager().GetHighestOSMZoom())
            {
                VectorTileBuffer buffer = new VectorTileBuffer(graphicsDevice, sector);
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
                buffer.Add(graphicsDevice, OSMBufferGenerator.GetLakesBorder(graphicsDevice, blobs, sector), sector);
                Console.WriteLine($"{sector} lakes loaded in {sw.Elapsed.TotalSeconds} seconds.");
                sw.Restart();
                buffer.Add(graphicsDevice, OSMBufferGenerator.GetRoads(graphicsDevice, blobs), sector);
                Console.WriteLine($"{sector} roads loaded in {sw.Elapsed.TotalSeconds} seconds.");
                sw.Restart();
                buffer.Add(graphicsDevice, OSMBufferGenerator.GetTrees(graphicsDevice, blobs, sector), sector);
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
                        size = 512 >> (roadSectors[i].Zoom - sector.Zoom);
                        x = sector.GetRelativeXOf(roadSectors[i]) * size;
                        y = sector.GetRelativeYOf(roadSectors[i]) * size;
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
                if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                {
                    using (var writer = File.OpenWrite(Path.Combine(OSMPaths.GetRenderRoot(), fileName)))
                    {
                        rendered.SaveAsPng(writer, rendered.Width, rendered.Height);
                    }
                }
                return new ImageTileBuffer(graphicsDevice, rendered, sector);
            }
        }
    }
}

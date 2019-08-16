using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            return File.Exists(OSMPaths.GetSectorImagePath(sector));
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
            foreach (var file in Directory.EnumerateFiles(OSMPaths.GetRenderRoot(), "*", SearchOption.AllDirectories))
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
            try
            {
                if (File.Exists(OSMPaths.GetSectorImagePath(sector)))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(OSMPaths.GetSectorImagePath(sector)))) Directory.CreateDirectory(Path.GetDirectoryName(OSMPaths.GetSectorImagePath(sector)));
                    using (var reader = File.OpenRead(OSMPaths.GetSectorImagePath(sector)))
                    {
                        return new ImageTileBuffer(graphicsDevice, Texture2D.FromStream(graphicsDevice, reader), sector);
                    }
                }
            }
            catch (Exception ex)
            {
                // image must've been corrupt
            }
            // otherwise, build it
            if (sector.Zoom >= ZCoords.GetSectorManager().GetHighestOSMZoom())
            {
                try
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
                    //buffer.Add(graphicsDevice, OSMBufferGenerator.GetTrees(graphicsDevice, blobs, sector), sector);
                    Console.WriteLine($"{sector} trees loaded in {sw.Elapsed.TotalSeconds} seconds.");
                    if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                    {
                        SuperSave(buffer.GetImage(graphicsDevice), OSMPaths.GetSectorImagePath(sector));
                    }
                    return buffer;
                }
                catch (Exception ex)
                {
                    if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                    {
                        SuperSave(GlobalContent.Error, OSMPaths.GetSectorImagePath(sector));
                    }
                    return new ImageTileBuffer(graphicsDevice, GlobalContent.Error, sector);
                }
            }
            else
            {
                // combination image
                Texture2D rendered = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
                List<ISector> roadSectors = sector.GetChildrenAtLevel(sector.Zoom + 1);
                Texture2D[] textures = new Texture2D[roadSectors.Count];
                for (int i = 0; i < roadSectors.Count; i++)
                {
                    IGraphicsBuffer buffer = null;
                    try
                    {
                        buffer = GetGraphicsBuffer(graphicsDevice, roadSectors[i]);
                        textures[i] = buffer.GetImage(graphicsDevice);
                    }
                    finally
                    {
                        if (buffer != null && !(buffer is ImageTileBuffer)) buffer.Dispose();
                    }
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
                            GraphicsBasic.DrawSpriteRect(graphicsDevice, x, y, size, size, textures[i], BlendState.AlphaBlend, Microsoft.Xna.Framework.Color.White);
                        }
                    }
                }
                for (int i = 0; i < textures.Length; i++)
                {
                    if (textures[i] != null && textures[i] != GlobalContent.Error) textures[i].Dispose();
                }
                if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                {
                    SuperSave(rendered, OSMPaths.GetSectorImagePath(sector));
                }
                return new ImageTileBuffer(graphicsDevice, rendered, sector);
            }
        }

        // keep trying to save the texture until it doesn't mess up
        private void SuperSave(Texture2D texture, string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using (var writer = File.OpenWrite(path))
                    {
                        texture.SaveAsPng(writer, texture.Width, texture.Height);
                        return;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            Console.WriteLine("texture was unsaved");
        }
    }
}
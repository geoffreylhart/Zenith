using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    class OSMSectorLoader
    {
        public IGraphicsBuffer GetCacheBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            return GetBuffer(graphicsDevice, sector, true);
        }

        public IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            return GetBuffer(graphicsDevice, sector, false);
        }

        private IGraphicsBuffer GetBuffer(GraphicsDevice graphicsDevice, ISector sector, bool cached)
        {
            if (sector.Zoom > ZCoords.GetSectorManager().GetHighestOSMZoom()) throw new NotImplementedException();
            if (ZCoords.GetSectorManager().GetHighestCacheZoom() > ZCoords.GetSectorManager().GetHighestOSMZoom()) throw new NotImplementedException();

            try
            {
                if (File.Exists(OSMPaths.GetSectorImagePath(sector)) && (cached || sector.Zoom != ZCoords.GetSectorManager().GetHighestOSMZoom()))
                {
                    using (var reader = File.OpenRead(OSMPaths.GetSectorImagePath(sector)))
                    {
                        return new ImageTileBuffer(graphicsDevice, Texture2D.FromStream(graphicsDevice, reader), sector);
                    }
                }
            }
            catch (Exception ex)
            {
                // sometimes the image is corrupt (or zero bytes)
            }
            // otherwise, build it
            if (sector.Zoom == ZCoords.GetSectorManager().GetHighestOSMZoom())
            {
                if (cached)
                {
                    // TODO: somehow all of this still breaks often and is pretty slow, but at least we only have to run it once
                    if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                    {
                        SuperSave(GlobalContent.Error, OSMPaths.GetSectorImagePath(sector));
                        //RebuildImage(graphicsDevice, sector);
                    }
                    return new ImageTileBuffer(graphicsDevice, GlobalContent.Error, sector);
                }
                else
                {
                    //try
                    //{
                        ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        buffer.LoadLinesFromFile();
                        buffer.GenerateVertices();
                        buffer.GenerateBuffers(graphicsDevice);
                        Console.WriteLine($"Total load time for {sector} is {sw.Elapsed.TotalSeconds} s");
                        if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                        {
                            using (var image = buffer.GetImage(graphicsDevice))
                            {
                                SuperSave(image, OSMPaths.GetSectorImagePath(sector));
                            }
                            RebuildImage(graphicsDevice, sector);
                        }
                        return buffer;
                    //}
                    //catch (Exception ex)
                    //{
                    //    if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                    //    {
                    //        SuperSave(GlobalContent.Error, OSMPaths.GetSectorImagePath(sector));
                    //        RebuildImage(graphicsDevice, sector);
                    //    }
                    //    return new ImageTileBuffer(graphicsDevice, GlobalContent.Error, sector);
                    //}
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void RebuildImage(GraphicsDevice graphicsDevice, ISector sector)
        {
            // combination images
            using (Texture2D rendered = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24))
            {
                int highestZoom = ZCoords.GetSectorManager().GetHighestCacheZoom();
                foreach (var parent in sector.GetAllParents().OrderBy(x => -x.Zoom).Where(x => x.Zoom <= highestZoom))
                {
                    List<ISector> roadSectors = parent.GetChildrenAtLevel(parent.Zoom == highestZoom - 1 ? ZCoords.GetSectorManager().GetHighestOSMZoom() : parent.Zoom + 1);
                    Texture2D[] textures = new Texture2D[roadSectors.Count];
                    for (int i = 0; i < roadSectors.Count; i++)
                    {
                        IGraphicsBuffer buffer = null;
                        if (File.Exists(OSMPaths.GetSectorImagePath(roadSectors[i])))
                        {
                            using (var reader = File.OpenRead(OSMPaths.GetSectorImagePath(roadSectors[i])))
                            {
                                buffer = new ImageTileBuffer(graphicsDevice, Texture2D.FromStream(graphicsDevice, reader), roadSectors[i]);
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        textures[i] = buffer.GetImage(graphicsDevice);
                    }
                    if (textures.Any(x => x != null))
                    {
                        graphicsDevice.SetRenderTarget((RenderTarget2D)rendered);
                        for (int i = 0; i < roadSectors.Count; i++)
                        {
                            int size, x, y;
                            size = 512 >> (roadSectors[i].Zoom - parent.Zoom);
                            x = parent.GetRelativeXOf(roadSectors[i]) * size;
                            y = parent.GetRelativeYOf(roadSectors[i]) * size;
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
                    SuperSave(rendered, OSMPaths.GetSectorImagePath(parent));
                }
            }
        }

        // keep trying to save the texture until it doesn't mess up
        // TODO: still doesn't work??
        public static void SuperSave(Texture2D texture, string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            // https://stackoverflow.com/questions/19248018/texture2d-saveaspng-memory-leak
            // what I originally tested this on was the texture for LE,X=67,Y=20,Zoom=7.PNG, a 206,940 byte PNG when saved using our new method (the other one failed at 245,817)
            Save(texture, texture.Width, texture.Height, ImageFormat.Png, path);
        }

        private static void Save(Texture2D texture, int width, int height, ImageFormat imageFormat, string filename)
        {
            using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                byte blue;
                IntPtr safePtr;
                BitmapData bitmapData;
                var rect = new System.Drawing.Rectangle(0, 0, width, height);
                byte[] textureData = new byte[4 * width * height];

                texture.GetData<byte>(textureData);
                for (int i = 0; i < textureData.Length; i += 4)
                {
                    blue = textureData[i];
                    textureData[i] = textureData[i + 2];
                    textureData[i + 2] = blue;
                }
                bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                safePtr = bitmapData.Scan0;
                Marshal.Copy(textureData, 0, safePtr, textureData.Length);
                bitmap.UnlockBits(bitmapData);
                bitmap.Save(filename, imageFormat);
            }
        }
    }
}
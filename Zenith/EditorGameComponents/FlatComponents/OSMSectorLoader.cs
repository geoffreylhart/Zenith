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
        public override IGraphicsBuffer GetCacheBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            return GetBuffer(graphicsDevice, sector, true);
        }

        public override IGraphicsBuffer GetGraphicsBuffer(GraphicsDevice graphicsDevice, ISector sector)
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
                    try
                    {
                        ProceduralTileBuffer buffer = new ProceduralTileBuffer(sector);
                        buffer.LoadLinesFromFile();
                        buffer.GenerateVertices();
                        buffer.GenerateBuffers(graphicsDevice);
                        if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                        {
                            SuperSave(buffer.GetImage(graphicsDevice), OSMPaths.GetSectorImagePath(sector));
                            RebuildImage(graphicsDevice, sector);
                        }
                        return buffer;
                    }
                    catch (Exception ex)
                    {
                        if (sector.Zoom <= ZCoords.GetSectorManager().GetHighestCacheZoom())
                        {
                            SuperSave(GlobalContent.Error, OSMPaths.GetSectorImagePath(sector));
                            RebuildImage(graphicsDevice, sector);
                        }
                        return new ImageTileBuffer(graphicsDevice, GlobalContent.Error, sector);
                    }
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
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
            String filePath = @"..\..\..\..\LocalCache\GoogleMaps\COMPOSITE\" + fileName;
            return File.Exists(filePath);
        }

        public override IEnumerable<Sector> EnumerateCachedSectors()
        {
            foreach (var file in Directory.EnumerateFiles(@"..\..\..\..\LocalCache\GoogleMaps\COMPOSITE"))
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
            if (File.Exists(@"..\..\..\..\LocalCache\GoogleMaps\COMPOSITE\" + fileName))
            {
                using (var reader = File.OpenRead(@"..\..\..\..\LocalCache\GoogleMaps\COMPOSITE\" + fileName))
                {
                    return Texture2D.FromStream(graphicsDevice, reader);
                }
            }
            // otherwise, build it
            Texture2D composite = MakeComposite(graphicsDevice, sector);
            using (var writer = File.OpenWrite(@"..\..\..\..\LocalCache\GoogleMaps\Composite\" + fileName))
            {
                composite.SaveAsPng(writer, composite.Width, composite.Height);
            }
            return composite;
        }

        private Texture2D MakeComposite(GraphicsDevice graphicsDevice, Sector sector)
        {
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            Texture2D water = GetMap(graphicsDevice, sector, MapGenerator.MapStyle.LAND_WATER_ONLY, true);
            GraphicsBasic.DrawScreenRect(graphicsDevice, 0, 0, 512, 512, Color.Blue);
            BlendState bmMask = new BlendState()
            {
                AlphaBlendFunction = BlendFunction.Add,
                AlphaDestinationBlend = Blend.InverseSourceAlpha,
                AlphaSourceBlend = Blend.One,
                BlendFactor = new Color(0,0,0,0),
                ColorBlendFunction = BlendFunction.Add,
                ColorDestinationBlend = Blend.InverseSourceAlpha, // normally InverseSourceAlpha (opaque)
                ColorSourceBlend = Blend.One, // normally One (opaque)
                ColorWriteChannels = ColorWriteChannels.All,
                ColorWriteChannels1 = ColorWriteChannels.All,
                ColorWriteChannels2 = ColorWriteChannels.All,
                ColorWriteChannels3 = ColorWriteChannels.All,
                IndependentBlendEnable = false,
                MultiSampleMask = 2147483647
            };
            GraphicsBasic.DrawSpriteRect(graphicsDevice, 0, 0, 512, 512, water, bmMask, Color.White);
            return newTarget;
        }

        private Texture2D GetMap(GraphicsDevice graphicsDevice, Sector sector, MapGenerator.MapStyle mapStyle, bool save)
        {
            String fileName = sector.ToString() + ".PNG";
            String styleStr = mapStyle.ToString().ToUpper();
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

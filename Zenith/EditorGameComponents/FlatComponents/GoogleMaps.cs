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
using Zenith.ZMath;

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

        public override bool DoAutoLoad(Sector sector)
        {
            return false;
        }

        public override bool AllowUnload(Sector sector)
        {
            return false;
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
            //using (var writer = File.OpenWrite(@"..\..\..\..\LocalCache\GoogleMaps\Composite\" + fileName))
            //{
            //    composite.SaveAsPng(writer, composite.Width, composite.Height);
            //}
            return composite;
        }

        private Texture2D MakeComposite(GraphicsDevice graphicsDevice, Sector sector)
        {
            //LoadTonsOfMaps(graphicsDevice);
            RenderTarget2D newTarget = new RenderTarget2D(graphicsDevice, 512, 512, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            graphicsDevice.SetRenderTarget(newTarget);
            List<Sector> waterSectors = sector.GetChildrenAtLevel(Math.Min(2 + sector.zoom, 15));
            waterSectors.AddRange(sector.GetAllParents());
            waterSectors.Add(sector);
            waterSectors.Sort((x, y) => x.zoom.CompareTo(y.zoom));
            List<Sector> roadSectors = new List<Sector>();
            List<Sector> plotBuildingSectors = new List<Sector>();
            if (sector.zoom >= 11) roadSectors.AddRange(sector.GetChildrenAtLevel(15));
            if (sector.zoom >= 13) plotBuildingSectors.AddRange(sector.GetChildrenAtLevel(17));
            roadSectors.AddRange(sector.GetAllParents());
            plotBuildingSectors.Add(sector);
            roadSectors.AddRange(sector.GetAllParents());
            plotBuildingSectors.Add(sector);
            GraphicsBasic.DrawScreenRect(graphicsDevice, 0, 0, 512, 512, Pallete.OCEAN_BLUE);
            foreach (var s in waterSectors)
            {
                int size, x, y;
                if (s.zoom >= sector.zoom)
                {
                    int subd = 1 << (s.zoom - sector.zoom);
                    size = 512 >> (s.zoom - sector.zoom);
                    x = sector.GetRelativeXOf(s) * size;
                    y = (subd - 1 - sector.GetRelativeYOf(s)) * size;
                }
                else
                {
                    int subd = 1 << (sector.zoom - s.zoom);
                    size = 512 << (sector.zoom - s.zoom);
                    x = -s.GetRelativeXOf(sector) * 512;
                    y = -(subd - 1 - s.GetRelativeYOf(sector)) * 512;
                }
                Texture2D water = LoadMap(graphicsDevice, s, MapGenerator.MapStyle.LAND_WATER_ONLY);
                if (water != null)
                {
                    GraphicsBasic.DrawScreenRect(graphicsDevice, x, y, size, size, Pallete.OCEAN_BLUE);
                    SpriteBatchBasic.DrawColorWithMask(graphicsDevice, x, y, size, size, water, Pallete.GRASS_GREEN);
                }
            }
            foreach (var s in roadSectors)
            {
                int size, x, y;
                if (s.zoom >= sector.zoom)
                {
                    int subd = 1 << (s.zoom - sector.zoom);
                    size = 512 >> (s.zoom - sector.zoom);
                    x = sector.GetRelativeXOf(s) * size;
                    y = (subd - 1 - sector.GetRelativeYOf(s)) * size;
                }
                else
                {
                    int subd = 1 << (sector.zoom - s.zoom);
                    size = 512 << (sector.zoom - s.zoom);
                    x = -s.GetRelativeXOf(sector) * 512;
                    y = -(subd - 1 - s.GetRelativeYOf(sector)) * 512;
                }
                Texture2D road = LoadMap(graphicsDevice, s, MapGenerator.MapStyle.ROADS_ONLY);
                if (road != null)
                {
                    SpriteBatchBasic.DrawColorWithMask(graphicsDevice, x, y, size, size, road, Color.White);
                }
            }
            foreach (var s in plotBuildingSectors)
            {
                int size, x, y;
                if (s.zoom >= sector.zoom)
                {
                    int subd = 1 << (s.zoom - sector.zoom);
                    size = 512 >> (s.zoom - sector.zoom);
                    x = sector.GetRelativeXOf(s) * size;
                    y = (subd - 1 - sector.GetRelativeYOf(s)) * size;
                }
                else
                {
                    int subd = 1 << (sector.zoom - s.zoom);
                    size = 512 << (sector.zoom - s.zoom);
                    x = -s.GetRelativeXOf(sector) * 512;
                    y = -(subd - 1 - s.GetRelativeYOf(sector)) * 512;
                }
                Texture2D plots = LoadMap(graphicsDevice, s, MapGenerator.MapStyle.PLOTS_ONLY);
                Texture2D buildings = LoadMap(graphicsDevice, s, MapGenerator.MapStyle.BUILDINGS_ONLY);
                if (plots != null)
                {
                    SpriteBatchBasic.DrawColorWithInvertedMask(graphicsDevice, x, y, size, size, plots, Color.White, 0, 255);
                }
                if (buildings != null)
                {
                    SpriteBatchBasic.DrawColorWithInvertedMask(graphicsDevice, x, y, size, size, buildings, Color.White, 240, 248);
                }
            }
            return newTarget;
        }

        private Texture2D LoadMap(GraphicsDevice graphicsDevice, Sector sector, MapGenerator.MapStyle mapStyle)
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
                return null;
            }
        }

        // just get from google, don't load it into memory or anything
        private void SaveMap(GraphicsDevice graphicsDevice, Sector sector, MapGenerator.MapStyle mapStyle)
        {
            String fileName = sector.ToString() + ".PNG";
            String styleStr = mapStyle.ToString().ToUpper();
            if (!File.Exists(@"..\..\..\..\LocalCache\GoogleMaps\" + styleStr + "\\" + fileName))
            {
                Texture2D texture = MapGenerator.GetMap(graphicsDevice, sector, mapStyle);
                using (var writer = File.OpenWrite(@"..\..\..\..\LocalCache\GoogleMaps\" + styleStr + "\\" + fileName))
                {
                    texture.SaveAsPng(writer, texture.Width, texture.Height);
                }
                texture.Dispose();
            }
        }

        // currently Google gives $200/mo credit
        // I've used $6.13 credit (3064 requests)
        // normally $200/mo is enough to get 100000 maps ($2 per 1000)
        // cut that in half due to the way I download maps
        // my quota is currently set to like 25000 a day
        private void LoadTonsOfMaps(GraphicsDevice graphicsDevice)
        {
            //List<Sector> globalSectors = GetSectorsOfMaxSize(new Sector(0,0,0), 1.0/128); // 352
            List<Sector> pensacolaSectors = GetSectorsAround(new Sector(0, 0, 0), -87.3294527 * Math.PI / 180, 30.4668536 * Math.PI / 180, 15, 1.0 / 700); // 382
            List<Sector> pensacolaSectors2 = GetSectorsAroundOfSize(new Sector(0, 0, 0), -87.3294527 * Math.PI / 180, 30.4668536 * Math.PI / 180, 15, 1.0 / 700); // 271
            // estimated 19374 google pings for Pensacola, almost entirely due to plot/buildings
            foreach (var s in pensacolaSectors)
            {
                SaveMap(graphicsDevice, s, MapGenerator.MapStyle.LAND_WATER_ONLY);
                SaveMap(graphicsDevice, s, MapGenerator.MapStyle.SATELLITE);
            }
            foreach (var s in pensacolaSectors2)
            {
                SaveMap(graphicsDevice, s, MapGenerator.MapStyle.ROADS_ONLY);
                foreach (var c in s.GetChildrenAtLevel(17))
                {
                    SaveMap(graphicsDevice, c, MapGenerator.MapStyle.PLOTS_ONLY);
                    SaveMap(graphicsDevice, c, MapGenerator.MapStyle.BUILDINGS_ONLY);
                }
            }
        }

        // get progressively smaller sectors around a target
        public static List<Sector> GetSectorsAround(Sector root, double longitude, double latitude, int maxZ, double radius)
        {
            List<Sector> answer = new List<Sector>();
            double distance = root.MinDistanceFrom(new LongLat(longitude, latitude));
            double preferredSize = Math.Max(distance - radius, 0); // surprising, huh?
            if (root.SurfaceAreaPortion <= preferredSize || root.zoom >= maxZ)
            {
                answer.Add(root);
            }
            else
            {
                foreach (var rel in root.GetChildrenAtLevel(root.zoom + 1))
                {
                    answer.AddRange(GetSectorsAround(rel, longitude, latitude, maxZ, radius));
                }
            }
            return answer;
        }

        // get sectors of a specific size around a target
        public static List<Sector> GetSectorsAroundOfSize(Sector root, double longitude, double latitude, int size, double radius)
        {
            List<Sector> answer = new List<Sector>();
            double distance = root.MinDistanceFrom(new LongLat(longitude, latitude));
            if (distance <= radius)
            {
                if (root.zoom >= size)
                {
                    answer.Add(root);
                }
                else
                {
                    foreach (var rel in root.GetChildrenAtLevel(root.zoom + 1))
                    {
                        answer.AddRange(GetSectorsAroundOfSize(rel, longitude, latitude, size, radius));
                    }
                }
            }
            return answer;
        }

        // gets all sectors with a surface area below "size"
        public static List<Sector> GetSectorsOfMaxSize(Sector root, double size)
        {
            List<Sector> answer = new List<Sector>();
            if (root.SurfaceAreaPortion <= size)
            {
                answer.Add(root);
            }
            else
            {
                foreach (var rel in root.GetChildrenAtLevel(root.zoom + 1))
                {
                    answer.AddRange(GetSectorsOfMaxSize(rel, size));
                }
            }
            return answer;
        }
    }
}

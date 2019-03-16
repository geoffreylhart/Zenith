using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    class CustomMaps : SectorLoader
    {
        public override bool CacheExists(Sector sector)
        {
            String fileName = sector.ToString() + ".PNG";
            String filePath = @"..\..\..\..\LocalCache\CustomMaps\" + fileName;
            return File.Exists(filePath);
        }

        public override IEnumerable<Sector> EnumerateCachedSectors()
        {
            foreach (var file in Directory.EnumerateFiles(@"..\..\..\..\LocalCache\CustomMaps"))
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
            String filePath = @"..\..\..\..\LocalCache\CustomMaps\" + fileName;
            if (File.Exists(filePath))
            {
                using (var reader = File.OpenRead(filePath))
                {
                    return Texture2D.FromStream(graphicsDevice, reader);
                }
            }
            else
            {
                if (!File.Exists(@"..\..\..\..\LocalCache\CustomMaps\X=131,Y=301,Zoom=9.svg")) GetTextureFromSVG(graphicsDevice, new Sector(131, 301, 9), new Sector(131, 301, 9)).Dispose();
                if (new Sector(131, 301, 9).ContainsSector(sector))
                {
                    return GetTextureFromSVG(graphicsDevice, sector, new Sector(131, 301, 9));
                }
                return GetTextureFromSVG(graphicsDevice, sector, new Sector(0, 0, 0));
            }
        }

        private Texture2D GetTextureFromSVG(GraphicsDevice graphicsDevice, Sector target, Sector src)
        {
            String fileName = target.ToString() + ".PNG";
            String filePath = @"..\..\..\..\LocalCache\CustomMaps\" + fileName;
            // render it using InkScapes help
            Process exe = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"C:\Program Files\Inkscape\inkscape.com";
            String srcPath = @"C:\Users\Geoffrey Hart\Documents\Visual Studio 2017\Projects\Zenith\Zenith\GraphicsSource\InkScape\CustomMaps\" + src.ToString() + ".svg";
            String dest = filePath;
            // remember that inkscape has 0,0 in the lower-left corner
            double size = (target.ZoomPortion / src.ZoomPortion) * 512;
            double x1 = (target.x - src.x * src.ZoomPortion / target.ZoomPortion) * size;
            double y1 = (target.y - src.y * src.ZoomPortion / target.ZoomPortion) * size;
            double x2 = (target.x + 1 - src.x * src.ZoomPortion / target.ZoomPortion) * size;
            double y2 = (target.y + 1 - src.y * src.ZoomPortion / target.ZoomPortion) * size;
            startInfo.Arguments = $"-z \"{srcPath}\" -e {dest} -a {x1}:{y1}:{x2}:{y2} -w 512 -h 512";
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            exe.StartInfo = startInfo;
            exe.Start();
            exe.WaitForExit();
            using (var reader = File.OpenRead(filePath))
            {
                return Texture2D.FromStream(graphicsDevice, reader);
            }
        }
    }
}

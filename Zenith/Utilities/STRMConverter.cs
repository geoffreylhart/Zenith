using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Zenith.MathHelpers;
using Zenith.ZMath;

namespace Zenith.Utilities
{
    class STRMConverter
    {
        // The tiles are distributed as zip files containing HGT files labeled with the coordinate of the southwest cell. For example, the file N20E100.hgt contains data from 20°N to 21°N and from 100°E to 101°E inclusive.
        // The HGT files have a very simple format. Each file is a series of 16-bit integers giving the height of each cell in meters arranged from west to east and then north to south
        internal static void ConvertHGTZIPToPNG(string inputPath, string outputPath)
        {
            // unzip to get bytes
            byte[] bytes = Compression.UnZipToBytes(inputPath);
            //
            int W, H;
            if (inputPath.Contains("hgt"))
            {
                int size = (int)Math.Sqrt(bytes.Length / 2);
                W = size;
                H = size;
            }
            else
            {
                W = 600 * 8;
                H = 750 * 8;
            }
            if (W * H * 2 != bytes.Length) throw new NotImplementedException();
            Bitmap bitmap = new Bitmap(W, H, PixelFormat.Format24bppRgb);
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    int dx = x == 0 ? 0 : GetShort(bytes, x, y, W, H) - GetShort(bytes, x - 1, y, W, H);
                    int dy = y == 0 ? 0 : GetShort(bytes, x, y, W, H) - GetShort(bytes, x, y - 1, W, H);
                    Color c = GetColor(GetShort(bytes, x, y, W, H), dx, dy);
                    bitmap.SetPixel(x, y, c);
                }
            }
            bitmap.Save(outputPath);
        }

        internal static void ConvertHGTZIPsToPNG(ISector sector, string outputPath)
        {
            int BUFFER_SIZE = 10;
            var fileBytes = new List<KeyValuePair<string, byte[]>>();
            int REZ = 512;
            int[,] shorts = new int[REZ, REZ];
            var exists = new HashSet<string>();
            var doesntexist = new HashSet<string>();
            for (int x = 0; x < REZ; x++)
            {
                for (int y = 0; y < REZ; y++)
                {
                    // for now, nearest-neighbor
                    var longLat = new SphereVector(sector.ProjectToSphereCoordinates(new Vector2d((0.5 + x) / REZ, (0.5 + y) / REZ))).ToLongLat() * 180 / Math.PI;
                    string filePath;
                    double px, py;
                    // ex: N00E017.SRTMGL1.hgt.zip (note, file name is the coordinate of bottom-left most point)
                    // for some reason for dem, the file name is the coordinate of the top-left most point?
                    if (longLat.Y < -60 || longLat.Y > 60)
                    {
                        int roundX = ((int)((longLat.X + 420) / 40)) * 40 - 420;
                        int roundY = ((int)Math.Ceiling((longLat.Y + 510) / 50)) * 50 - 510;
                        filePath = Path.Combine(@"C:\Users\Geoffrey Hart\Downloads\Source\STRMGL30", $"{(roundX > 0 ? "E" : "W")}{Math.Abs(roundX):D3}{(roundY > 0 ? "N" : "S")}{Math.Abs(roundY):D2}.SRTMGL30.dem.zip");
                        px = (longLat.X - roundX) / 40;
                        py = (1 - (roundY - longLat.Y) / 50);
                    }
                    else
                    {
                        filePath = Path.Combine(@"C:\Users\Geoffrey Hart\Downloads\Source\STRMGL1", $"{(longLat.Y > 0 ? "N" : "S")}{(int)Math.Abs(Math.Floor(longLat.Y)):D2}{(longLat.X > 0 ? "E" : "W")}{(int)Math.Abs(Math.Floor(longLat.X)):D3}.SRTMGL1.hgt.zip");
                        if (!exists.Contains(filePath) && !doesntexist.Contains(filePath))
                        {
                            if (File.Exists(filePath))
                            {
                                exists.Add(filePath);
                            }
                            else
                            {
                                doesntexist.Add(filePath);
                            }
                        }
                        if (exists.Contains(filePath))
                        {
                            px = (longLat.X + 360) % 1;
                            py = (longLat.Y + 360) % 1;
                        }
                        else
                        {
                            int roundX = ((int)((longLat.X + 420) / 40)) * 40 - 420;
                            int roundY = ((int)((longLat.Y + 510) / 50)) * 50 - 510;
                            filePath = Path.Combine(@"C:\Users\Geoffrey Hart\Downloads\Source\STRMGL30", $"{(roundX > 0 ? "E" : "W")}{Math.Abs(roundX):D3}{(roundY > 0 ? "N" : "S")}{Math.Abs(roundY):D2}.SRTMGL30.dem.zip");
                            px = (longLat.X - roundX) / 40;
                            py = (longLat.Y - roundY) / 50;
                        }
                    }
                    if (!fileBytes.Any(z => z.Key == filePath))
                    {
                        if (fileBytes.Count == BUFFER_SIZE) fileBytes.RemoveAt(0);
                        fileBytes.Add(new KeyValuePair<string, byte[]>(filePath, Compression.UnZipToBytes(filePath)));
                    }
                    var bytes = fileBytes.Where(z => z.Key == filePath).Single().Value;
                    int W, H;
                    if (filePath.Contains("hgt"))
                    {
                        int size = (int)Math.Sqrt(bytes.Length / 2);
                        W = size;
                        H = size;
                    }
                    else
                    {
                        W = 600 * 8;
                        H = 750 * 8;
                    }
                    if (W * H * 2 != bytes.Length) throw new NotImplementedException();
                    shorts[x, y] = (int)Sample(bytes, px * (W - 1), (1 - py) * (H - 1), W, H);
                }
            }
            Bitmap bitmap = new Bitmap(REZ, REZ, PixelFormat.Format24bppRgb);
            for (int x = 0; x < REZ; x++)
            {
                for (int y = 0; y < REZ; y++)
                {
                    int dx = x == 0 ? 0 : shorts[x, y] - shorts[x - 1, y];
                    int dy = y == 0 ? 0 : shorts[x, y] - shorts[x, y - 1];
                    Color c = GetColor(shorts[x, y], dx, dy);
                    bitmap.SetPixel(x, y, c);
                }
            }
            bitmap.Save(outputPath);
        }
        private static double Sample(byte[] bytes, double x, double y, int w, int h)
        {
            if (x < 0) throw new NotImplementedException();
            if (y < 0) throw new NotImplementedException();
            if (x >= w - 1) throw new NotImplementedException();
            if (y >= h - 1) throw new NotImplementedException();
            double topLeft = GetShort(bytes, (int)x, (int)y, w, h);
            double topRight = GetShort(bytes, (int)x + 1, (int)y, w, h);
            double bottomLeft = GetShort(bytes, (int)x, (int)y + 1, w, h);
            double bottomRight = GetShort(bytes, (int)x + 1, (int)y + 1, w, h);
            // just do linear for now
            return ((1 - x % 1) * topLeft + (x % 1) * topRight) * (1 - y % 1) + ((1 - x % 1) * bottomLeft + (x % 1) * bottomRight) * (y % 1);
        }

        private static int GetShort(byte[] bytes, int x, int y, int w, int h)
        {
            return bytes[(w * y + x) * 2] * 256 + bytes[(w * y + x) * 2 + 1];
        }

        private static Color GetColor(int value, int dx = 0, int dy = 0)
        {
            Color color = Color.White;
            if (value <= 0)
            {
                color = Color.Blue;
            }
            else if (value <= 255)
            {
                // 1159 is min
                // 2989 is max
                //if (value < 1159 + 50) return Color.Blue;
                //if (value < 1159 + 100) return Color.Green;
                // current max is 2989
                color = Color.FromArgb(0, value / 2 + 128, 0);
            }
            else if (value <= 255 + 256)
            {
                color = Color.FromArgb(value - 256, 255, 0);
            }
            else
            {
                color = Color.White;
            }
            Vector3d lightDirection = new Vector3d(1, 1, -1); // from the topleft pointing into the image
            Vector3d slopeNormal = new Vector3d(-dx, -dy, 1);
            double brightness = (1 - lightDirection.Normalized().Dot(slopeNormal.Normalized())) / 2 * 0.8 + 0.2;
            return Color.FromArgb((int)(brightness * color.R), (int)(brightness * color.G), (int)(brightness * color.B));
        }
    }
}

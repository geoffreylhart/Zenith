using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
            int size = (int)Math.Sqrt(bytes.Length / 2);
            if (size * size * 2 != bytes.Length) throw new NotImplementedException();
            //BitmapData bitmapData;
            //var rect = new System.Drawing.Rectangle(0, 0, size, size);
            Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format24bppRgb);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int dx = x == 0 ? 0 : GetShort(bytes, x, y, size) - GetShort(bytes, x - 1, y, size);
                    int dy = y == 0 ? 0 : GetShort(bytes, x, y, size) - GetShort(bytes, x, y - 1, size);
                    Color c = GetColor(GetShort(bytes, x, y, size), dx, dy);
                    bitmap.SetPixel(x, y, c);
                }
            }
            bitmap.Save(outputPath);
        }

        internal static void ConvertHGTZIPsToPNG(ISector sector, string outputPath)
        {
            var fileBytes = new Dictionary<string, byte[]>();
            int REZ = 512;
            int[,] shorts = new int[REZ, REZ];
            for (int x = 0; x < REZ; x++)
            {
                for (int y = 0; y < REZ; y++)
                {
                    // for now, nearest-neighbor
                    var longLat = new SphereVector(sector.ProjectToSphereCoordinates(new Vector2d((0.5 + x) / REZ, (0.5 + y) / REZ))).ToLongLat() * 180 / Math.PI;
                    string filePath = Path.Combine(@"C:\Users\Geoffrey Hart\Downloads", $"{(longLat.Y > 0 ? "N" : "S")}{(int)Math.Abs(Math.Floor(longLat.Y)):D2}{(longLat.X > 0 ? "E" : "W")}{(int)Math.Abs(Math.Floor(longLat.X)):D3}.SRTMGL1.hgt.zip"); // ex: N00E017.SRTMGL1.hgt.zip (note, file name is coordinate of bottom-left most point)
                    if (!fileBytes.ContainsKey(filePath)) fileBytes[filePath] = Compression.UnZipToBytes(filePath);
                    var bytes = fileBytes[filePath];
                    int size = (int)Math.Sqrt(bytes.Length / 2);
                    if (size * size * 2 != bytes.Length) throw new NotImplementedException();
                    double px = (longLat.X + 360) % 1;
                    double py = (longLat.Y + 360) % 1;
                    shorts[x, y] = (int)Sample(bytes, px * (size - 1), (1 - py) * (size - 1), size);
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
        private static double Sample(byte[] bytes, double x, double y, int size)
        {
            if (x < 0) throw new NotImplementedException();
            if (y < 0) throw new NotImplementedException();
            if (x >= size - 1) throw new NotImplementedException();
            if (y >= size - 1) throw new NotImplementedException();
            double topLeft = GetShort(bytes, (int)x, (int)y, size);
            double topRight = GetShort(bytes, (int)x + 1, (int)y, size);
            double bottomLeft = GetShort(bytes, (int)x, (int)y + 1, size);
            double bottomRight = GetShort(bytes, (int)x + 1, (int)y + 1, size);
            // just do linear for now
            return ((1 - x % 1) * topLeft + (x % 1) * topRight) * (1 - y % 1) + ((1 - x % 1) * bottomLeft + (x % 1) * bottomRight) * (y % 1);
        }

        private static int GetShort(byte[] bytes, int x, int y, int size)
        {
            return bytes[(size * y + x) * 2] * 256 + bytes[(size * y + x) * 2 + 1];
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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework.Graphics;
using Zenith.MathHelpers;
using static System.Net.Mime.MediaTypeNames;

namespace Zenith
{
    public class MapGenerator
    {
        // latitude and longitude given in degrees
        // Google api doesn't like longitude outside of -180 and 180 or latitude outside of -90 and 90
        internal static Texture2D GetMap(GraphicsDevice graphicsDevice, double longitude, double latitude, int zoom)
        {
            LongLatHelper.NormalizeLongLatDegrees(ref longitude, ref latitude);
            var data = GetMapHQWithoutLogo(longitude, latitude, zoom);
            return Texture2D.FromStream(graphicsDevice, data);
        }

        private static Stream GetMapPlainly(double longitude, double latitude, int zoom)
        {
            using (WebClient wc = new WebClient())
            {
                // byte[] data = wc.DownloadData(String.Format($"https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&center={latitude},{longitude}&zoom={zoom}&size=256x256&scale=2&key=AIzaSyDePV-JaAnPp5S-2XV1TQgKiWIaMTgtXIo"));
                String style = "style=feature:landscape%7Celement:geometry.fill%7Ccolor:0x006000";
                style += "&style=feature:water%7Celement:geometry.fill%7Ccolor:0x000080";
                style += "&style=feature:all%7Celement:labels%7Cvisibility:off";
                style += "&style=feature:transit%7Cvisibility:off";
                style += "&style=feature:administrative%7Cvisibility:off";
                style += "&style=feature:transit%7Cvisibility:off";
                style += "&style=feature:poi%7Cvisibility:off";
                style += "&style=feature:road%7Cvisibility:off";
                return new MemoryStream(wc.DownloadData(String.Format($"https://maps.googleapis.com/maps/api/staticmap?center={latitude},{longitude}&{style}&zoom={zoom}&size=256x256&scale=2&key=AIzaSyDePV-JaAnPp5S-2XV1TQgKiWIaMTgtXIo")));
            }
        }

        private static int HQ_AMOUNT = 0; // zoom level
        private static Stream GetMapHQWithoutLogo(double longitude, double latitude, int zoom)
        {
            int pow = 1 << HQ_AMOUNT;
            using (var bitmap = new Bitmap(512 * pow, 512 * pow))
            {
                using (var canvas = Graphics.FromImage(bitmap))
                {
                    canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    for (int x = 0; x < pow; x++)
                    {
                        for (int y = 0; y < pow * 2; y++)
                        {
                            double offy = Math.Pow(0.5, zoom) / (pow * 2) * (y - pow + 1);
                            double offx = Math.Pow(0.5, zoom) / pow * (x - pow / 2.0 + 0.5);
                            double thisLat = ToLat(ToY(latitude * Math.PI / 180) - offy) * 180 / Math.PI;
                            double thisLong = longitude + offx * 360;
                            // google doesn't support longitude outside the rang -180 to 180
                            while (thisLong < -180)
                            {
                                thisLong += 360;
                            }
                            while (thisLong > 180)
                            {
                                thisLong -= 360;
                            }
                            var ms = GetMapPlainly(thisLong, thisLat, zoom + HQ_AMOUNT);
                            var img = System.Drawing.Image.FromStream(ms);
                            canvas.DrawImage(img, 512 * x, y * 512 / 2);
                        }
                    }
                    canvas.Save();
                }
                var memStream = new MemoryStream();
                bitmap.Save(memStream, ImageFormat.Png);
                return memStream;
            }
        }


        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }

        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }
    }
}

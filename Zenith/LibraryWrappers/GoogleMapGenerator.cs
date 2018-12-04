using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework.Graphics;
using Zenith.MathHelpers;
using static System.Net.Mime.MediaTypeNames;
using static Zenith.EditorGameComponents.FlatComponents.SectorLoader;

namespace Zenith
{
    // limited to 25,000 requests a day for free
    public class MapGenerator
    {
        public enum MapStyle
        {
            SATELLITE,
            ROADS_ONLY,
            BUILDINGS_ONLY,
            LAND_WATER_ONLY
        }

        // latitude and longitude given in degrees
        // Google api doesn't like longitude outside of -180 and 180 or latitude outside of -90 and 90
        internal static Texture2D GetMap(GraphicsDevice graphicsDevice, Sector sector, MapStyle mapStyle)
        {
            var data = GetMapHQWithoutLogo(sector, mapStyle);
            return Texture2D.FromStream(graphicsDevice, data);
        }

        private static Stream GetMapPlainly(double longitude, double latitude, int zoom, MapStyle mapStyle)
        {
            using (WebClient wc = new WebClient())
            {
                String latStr = latitude.ToString();
                String longStr = longitude.ToString();
                if (latStr.Contains("E-")) latStr = "0";
                if (longStr.Contains("E-")) longStr = "0";
                String styleAndType = GetStyleAndTypeStr(zoom, mapStyle);
                return new MemoryStream(wc.DownloadData(String.Format($"https://maps.googleapis.com/maps/api/staticmap?center={latStr},{longStr}&zoom={zoom}&size=256x256&scale=2&key={GetAPIKey()}&{styleAndType}")));
            }
        }

        private static string GetStyleAndTypeStr(int zoom, MapStyle mapStyle)
        {
            String style = "";
            switch (mapStyle)
            {
                case MapStyle.SATELLITE:
                    return "maptype=satellite";
                case MapStyle.ROADS_ONLY:
                    return "https://maps.googleapis.com/maps/api/staticmap?key=YOUR_API_KEY&center=30.502376571918788,-87.32878473940382&zoom=17&format=png&maptype=roadmap&style=element:labels%7Cvisibility:off&style=feature:administrative%7Cvisibility:off&style=feature:administrative.land_parcel%7Ccolor:0xbfbfbf%7Cvisibility:on%7Cweight:2&style=feature:landscape%7Cvisibility:off&style=feature:poi%7Cvisibility:off&style=feature:road%7Celement:geometry.fill%7Ccolor:0xffffff&style=feature:road%7Celement:geometry.stroke%7Cvisibility:off&style=feature:transit%7Cvisibility:off&style=feature:water%7Cvisibility:off&size=480x360";
                case MapStyle.BUILDINGS_ONLY:
                    style += "&style=visibility:off";
                    style += "&style=feature:landscape.man_made%7Cvisibility:on";
                    return "maptype=roadmap" + style;
                case MapStyle.LAND_WATER_ONLY:
                    style += "&style=visibility:off";
                    style += "&style=feature:water%7Celement:geometry.fill%7Cvisibility:on";
                    return "maptype=roadmap" + style;
                default:
                    throw new NotImplementedException();
            }
        }

        private static int HQ_AMOUNT = 0; // zoom level
        private static Stream GetMapHQWithoutLogo(Sector sector, MapStyle mapStyle)
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
                            double offx = Math.Pow(0.5, sector.zoom) / pow * (x - pow / 2.0 + 0.5);
                            double offy = Math.Pow(0.5, sector.zoom) / (pow * 2) * (y - pow + 1);
                            double thisLong = sector.Longitude * 180 / Math.PI + offx * 360;
                            double thisLat = ToLat(ToY(sector.Latitude) - offy) * 180 / Math.PI;
                            // google doesn't support longitude outside the rang -180 to 180
                            while (thisLong < -180)
                            {
                                thisLong += 360;
                            }
                            while (thisLong > 180)
                            {
                                thisLong -= 360;
                            }
                            var ms = GetMapPlainly(thisLong, thisLat, sector.zoom + HQ_AMOUNT, mapStyle);
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

        // saved locally instead of being on GitHub
        private static String API_KEY;
        private static String GetAPIKey()
        {
            if (API_KEY != null) return API_KEY;
            API_KEY = File.ReadAllText(@"..\..\..\..\LocalCache\apikey.txt");
            return API_KEY;
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework.Graphics;
using Zenith.LibraryWrappers.OSM;
using Zenith.MathHelpers;
using Zenith.ZMath;
using static System.Net.Mime.MediaTypeNames;

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
            LAND_WATER_ONLY,
            PLOTS_ONLY
        }

        // latitude and longitude given in degrees
        // Google api doesn't like longitude outside of -180 and 180 or latitude outside of -90 and 90
        internal static Texture2D GetMap(GraphicsDevice graphicsDevice, MercatorSector sector, MapStyle mapStyle)
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
                    //roads first appear at 5
                    //6 / 7 barely changes
                    //non - highways appear at 8(lighting) ?
                    //  more at 9
                    //10 / 11 doesnt change much
                    //12, more roads(local)
                    //13, much better local roads
                    //14, even better
                    //15, best detail we need
                    style += "&style=visibility:off";
                    style += "&style=feature:landscape.natural%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on";
                    style += "&style=feature:road%7Celement:geometry.fill%7Ccolor:0xffffff%7Cvisibility:on";
                    return "maptype=roadmap" + style;
                case MapStyle.BUILDINGS_ONLY:
                    // only one which isn't b/w because styler won't let me
                    // always use zoom level 17 for buidlings (hidden otherwise)
                    style += "&style=visibility:off";
                    style += "&style=feature:landscape.man_made%7Cvisibility:on";
                    style += "&style=feature:landscape.natural%7Celement:geometry%7Ccolor:0xffffff%7Cvisibility:on";
                    return "maptype=roadmap" + style;
                case MapStyle.LAND_WATER_ONLY:
                    // we prefer to use 1 or 2 levls of HQ, to a max of 15 probably
                    style += "&style=visibility:off";
                    style += "&style=feature:landscape.natural%7Celement:geometry%7Ccolor:0xffffff%7Cvisibility:on";
                    style += "&style=feature:water%7Celement:geometry%7Ccolor:0x000000%7Cvisibility:on";
                    return "maptype=roadmap" + style;
                case MapStyle.PLOTS_ONLY:
                    // always use zoom level 17 for plots (hidden otherwise)
                    style += "&style=visibility:off";
                    style += "&style=feature:administrative.land_parcel%7Ccolor:0x000000%7Cvisibility:on";
                    style += "&style=feature:landscape.natural%7Celement:geometry%7Ccolor:0xffffff%7Cvisibility:on";
                    return "maptype=roadmap" + style;
                default:
                    throw new NotImplementedException();
            }
        }

        private static int HQ_AMOUNT = 0; // zoom level
        private static Stream GetMapHQWithoutLogo(MercatorSector sector, MapStyle mapStyle)
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
                            double offx = Math.Pow(0.5, sector.Zoom) / pow * (x - pow / 2.0 + 0.5);
                            double offy = Math.Pow(0.5, sector.Zoom) / (pow * 2) * (y - pow + 1);
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
                            var ms = GetMapPlainly(thisLong, thisLat, sector.Zoom + HQ_AMOUNT, mapStyle);
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
            API_KEY = File.ReadAllText(OSMPaths.GetLocalCacheRoot() + @"\LocalCache\apikey.txt");
            return API_KEY;
        }
    }
}

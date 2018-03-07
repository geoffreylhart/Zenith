using System;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework.Graphics;
using Zenith.MathHelpers;

namespace Zenith
{
    public class MapGenerator
    {
        // latitude and longitude given in degrees
        // Google api doesn't like longitude outside of -180 and 180 or latitude outside of -90 and 90
        internal static Texture2D GetMap(GraphicsDevice graphicsDevice, double longitude, double latitude, int zoom)
        {
            LongLatHelper.NormalizeLongLatDegrees(ref longitude, ref latitude);
            using (WebClient wc = new WebClient())
            {
                byte[] data = wc.DownloadData(String.Format("https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&center={0},{1}&zoom={2}&size=256x256&scale=2&key=AIzaSyDePV-JaAnPp5S-2XV1TQgKiWIaMTgtXIo", latitude, longitude, zoom));
                return Texture2D.FromStream(graphicsDevice, new MemoryStream(data));
            }
        }
    }
}

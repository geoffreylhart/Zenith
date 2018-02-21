using System;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith
{
    public class MapGenerator
    {
        internal static Texture2D GetMap(GraphicsDevice graphicsDevice, double latitude, double longitude, int zoom)
        {
            using (WebClient wc = new WebClient())
            {
                byte[] data = wc.DownloadData(String.Format("https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&center={0},{1}&zoom={2}&size=256x256&scale=2&key=AIzaSyDePV-JaAnPp5S-2XV1TQgKiWIaMTgtXIo", latitude, longitude, zoom));
                return Texture2D.FromStream(graphicsDevice, new MemoryStream(data));
            }
        }
    }
}

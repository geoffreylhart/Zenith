using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.MathHelpers
{
    public class LongLatHelper
    {
        public static void NormalizeLongLatRadians(ref double longitude, ref double latitude)
        {
            longitude *= 180 / Math.PI;
            latitude *= 180 / Math.PI;
            NormalizeLongLatDegrees(ref longitude, ref latitude);
            longitude /= 180 / Math.PI;
            latitude /= 180 / Math.PI;
        }

        // Keeps longitude within range of -180 and 180 and latitude within range of -90 and 90
        public static void NormalizeLongLatDegrees(ref double longitude, ref double latitude)
        {
            longitude = ((Math.Abs(longitude) + 180) % 360 - 180) * Math.Sign(longitude);
            latitude = ((Math.Abs(latitude) + 180) % 360 - 180) * Math.Sign(latitude);
            if (Math.Abs(latitude) > 90)
            {
                latitude = 180 * Math.Sign(latitude) - latitude;
                longitude += 180;
                if (longitude > 180) longitude -= 360;
            }
        }
    }
}

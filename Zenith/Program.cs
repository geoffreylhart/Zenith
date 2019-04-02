using System;
using System.Diagnostics;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZMath;

namespace Zenith
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // 115970
            int cnt1 = OSM.GetRoadsSlow(@"..\..\..\..\LocalCache\OpenStreetMaps\X=8,Y=18,Zoom=5\X=263,Y=602,Zoom=10.osm.pbf").Count;
            double time1 = sw.Elapsed.TotalSeconds;
            // currently 115970
            int cnt2 = OSM.GetRoadsFast(@"..\..\..\..\LocalCache\OpenStreetMaps\X=8,Y=18,Zoom=5\X=263,Y=602,Zoom=10.osm.pbf").Count;
            double time2 = sw.Elapsed.TotalSeconds - time1;
            // takes 57.25 minutes to churn through roads (without associating ways and nodes)
            // OSM.PrintHighwayIds(@"..\..\..\..\LocalCache\planet-latest.osm.pbf", @"..\..\..\..\LocalCache\highwayids.txt"); // took 38.49 minutes and 15.5 gb
            double time3 = sw.Elapsed.TotalSeconds - time2;
            using (var game = new Game1())
            {
                game.Run();
            }
        }
    }
}

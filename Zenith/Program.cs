using System;
using System.Diagnostics;
using Zenith.LibraryWrappers;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZMath;

namespace Zenith
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                OSMBufferGenerator.SegmentOSMPlanet();
                double timeHours = sw.Elapsed.TotalHours;
                game.Run();
            }
        }
    }
}

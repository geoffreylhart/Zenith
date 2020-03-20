using System;
using System.Diagnostics;
using Zenith.LibraryWrappers.OSM;

namespace ZenithCrossPlatform
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var i in new[] { 0, 1, 2, 3, 4, 5 })
            {
                new OSMMetaFinal().LoadAll("planet-meta" + i + ".data");
            }
            // bugs: front still has one grey line, bottom has a hole in the middle?
            double time = sw.Elapsed.TotalHours;
            using (var game = new Zenith.Game1())
                game.Run();
        }
    }
}

using System;
using System.Diagnostics;
using Zenith;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;

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
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Game1.RENDER_SECTOR = ZCoords.GetSectorManager().FromString(args[0]);
            }
            using (var game = new Zenith.Game1())
                game.Run();
        }

        public static void GeneratePlanetMeta()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var i in new[] { 0, 1, 2, 3, 4, 5 })
            {
                new OSMMetaFinal().LoadAll("planet-meta" + i + ".data");
            }
            double time = sw.Elapsed.TotalSeconds;
        }
    }
}

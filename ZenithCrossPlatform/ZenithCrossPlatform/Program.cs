using System;
using System.Diagnostics;
using Zenith;
using Zenith.LibraryWrappers.OSM;
using Zenith.Utilities;
using Zenith.ZGeom;
using Zenith.ZMath;

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
            //STRMConverter.ConvertHGTZIPToPNG(@"C:\Users\Geoffrey Hart\Downloads\temp\N03E019.SRTMGL1.hgt.zip", @"C:\Users\Geoffrey Hart\Downloads\temp\N03E019.png");
            //STRMConverter.ConvertHGTZIPToPNG(@"C:\Users\Geoffrey Hart\Downloads\N59W149.SRTMGL1.hgt.zip", @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage.png");
            //STRMConverter.ConvertHGTZIPToPNG(@"C:\Users\Geoffrey Hart\Downloads\N30W090.SRTMGL1.hgt.zip", @"C:\Users\Geoffrey Hart\Downloads\temp\f30.png");
            //STRMConverter.ConvertHGTZIPToPNG(@"C:\Users\Geoffrey Hart\Downloads\N29W090.SRTMGL1.hgt.zip", @"C:\Users\Geoffrey Hart\Downloads\temp\f29.png");
            //{[C:\Users\Geoffrey Hart\Downloads\N30W090.SRTMGL1.hgt.zip, {byte[25934402]}]}
            STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.LEFT, 128, 42, 8), @"C:\Users\Geoffrey Hart\Downloads\temp\florida.png");
            //if (args.Length > 0)
            //{
            //    Game1.RENDER_SECTOR = ZCoords.GetSectorManager().FromString(args[0]);
            //}
            //using (var game = new Zenith.Game1())
            //    game.Run();
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

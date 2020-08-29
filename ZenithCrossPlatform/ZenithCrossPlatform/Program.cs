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
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.LEFT, 128, 42, 8), @"C:\Users\Geoffrey Hart\Downloads\temp\florida.png");
            STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 80, 54, 8), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage.png");
            //STRMConverter.ConvertHGTZIPToPNG(@"C:\Users\Geoffrey Hart\Downloads\Source\STRMGL30\W180N40.SRTMGL30.dem.zip", @"C:\Users\Geoffrey Hart\Downloads\temp\debug.png");
            //STRMConverter.ConvertHGTZIPToPNG(@"C:\Users\Geoffrey Hart\Downloads\Source\STRMGL30\W180N90.SRTMGL30.dem.zip", @"C:\Users\Geoffrey Hart\Downloads\temp\debug2.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 40, 27, 7), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage1.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 20, 13, 6), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage2.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 10, 6, 5), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage3.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 5, 3, 4), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage4.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 2, 1, 3), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage5.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 1, 0, 2), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage6.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 0, 0, 1), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage7.png");
            //STRMConverter.ConvertHGTZIPsToPNG(new CubeSector(CubeSector.CubeSectorFace.TOP, 0, 0, 0), @"C:\Users\Geoffrey Hart\Downloads\temp\anchorage8.png");
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

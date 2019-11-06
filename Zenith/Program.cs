using OsmSharp.Streams;
using System;
using System.Diagnostics;
using System.IO;
using Zenith.LibraryWrappers;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZMath;

namespace Zenith
{
    public static class Program
    {
        public static string TO_LOAD = null;
        public static bool TERMINATE = false;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0) TO_LOAD = args[0];
            using (var game = new Game1())
            {
                game.Run();
            }
        }
    }
}

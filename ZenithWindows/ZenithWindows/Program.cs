using System;
using System.Collections.Generic;
using System.IO;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace ZenithWindows
{
#if WINDOWS || LINUX
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
            HelpZenithAndroid();
            using (var game = new Zenith.Game1())
            {
                game.Run();
            }
        }

        // move some of our files to the Android assets folder
        private static void HelpZenithAndroid()
        {
            LongLat longLat = new LongLat(-87.3294527 * Math.PI / 180, 30.4668536 * Math.PI / 180); // Pensacola
            CubeSector ourRoot = new CubeSector(CubeSector.CubeSectorFace.LEFT, 0, 0, 0);
            Vector2d relativeCoord = ourRoot.ProjectToLocalCoordinates(longLat.ToSphereVector());
            HashSet<ISector> sectorsToLoad = new HashSet<ISector>();
            foreach(var r in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                sectorsToLoad.Add(r);
                for (int z = 1; z <= 2; z++)
                {
                    foreach (var child in r.GetChildrenAtLevel(z))
                    {
                        sectorsToLoad.Add(child);
                    }
                }
            }
            for(int z = 3; z <= 8; z++)
            {
                ISector sector = ourRoot.GetSectorAt(relativeCoord.X, relativeCoord.Y, z);
                sectorsToLoad.Add(sector);
            }
            foreach (var sector in sectorsToLoad) MoveSectorImage(sector);
        }

        private static void MoveSectorImage(ISector sector)
        {
            string from = OSMPaths.GetSectorImagePath(sector);
            string to = OSMPaths.GetSectorImagePath(sector, Path.Combine(GetAndroidAssetsRoot(), @"OpenStreetMaps\Renders"));
            Directory.CreateDirectory(to.Substring(0, to.LastIndexOf('\\')));
            if(!File.Exists(to)) File.Copy(from, to);
        }

        public static string GetAndroidAssetsRoot()
        {
            string currDirectory = Directory.GetCurrentDirectory();
            return Path.Combine(currDirectory.Substring(0, currDirectory.IndexOf("Zenith")), @"Zenith\ZenithAndroid\ZenithAndroid\Assets");
        }
    }
#endif
}

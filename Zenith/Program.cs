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
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
            {
                game.Run();
            }
        }

        // current plan to rework sector loading:
        // first, primary desires:
        // we will start with no anticipatory logic
        // get rid of any "layer" logic and old google code, but keep the interface setup, just because it tends to keep me from going crazy with public/private stuff
        // are we fine with sector loading getting called from the draw step? yes, at least with texture loading stuff, we would love if you could somehow blitz through sectors at 120 fps
        //   - note: in that same vein, you might even expect to put player movement in the draw loop? unity would set "velocity" in the game loop, then animate that velocity accordingly
        // so heirarchy of steps as follows:
        //   - some class figures out which sectors are in view and how much of each, it will tell 6 flat sides where they get to render on its big texture and the portions of themselves that need rendering
        //     - this logic will be pretty specific to mercator/cubic
        //     - it will also tell them the appropriate zoom level
        // ...
    }
}

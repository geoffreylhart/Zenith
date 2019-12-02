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
            // test if instancing might help trees by increasing # of vertices in buffer
            // of course, try to instance houses
            // test vertex colors
            // test cube uv project or something to simplify
            // test shininess textures? (eventually)
            // test packed, specifically for super-simple textures
            // test embedded items with .fbx (eventually)
            // figure out best way to do inside/outside & LOD
            // - blender? python? external tool?
            // - be nice to have auto baked-in textures
            // - probably need 2.8
            // Now that we're using DirectX, try out multisampling on the backbuffer.
            // -unfortunately, that means we can't record? investigate getting it to work on non-backbuffer? (we could do all sorts of hacks for recording, though, such as FSAA)
            // Also, investigate FXAA shader.
        }
    }
}

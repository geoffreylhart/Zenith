using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    class Restructurer
    {
        FileStream[,] outputStreams = new FileStream[1024, 1024];
        FileStream nodeReader;
        long[] blobStarts; // the id of the first node in a blob (that contains nodes)
        long[] blobPos; // the file pos of the blob 

        internal void WorkOnRestructuring(string planetPath, string wayKey, string outputFolder)
        {
            for (int i = 0; i < 1024; i++)
            {
                for (int j = 0; j < 1024; j++)
                {
                    Sector parent = new Sector(i, j, 10);
                    Sector parent5 = new Sector(i / 32, j / 32, 5);
                    string folder = Path.Combine(outputFolder, parent5.ToString());
                    string outputPath = Path.Combine(outputFolder, parent5.ToString(), parent.ToString() + ".dat");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    outputStreams[i, j] = File.OpenWrite(outputPath);
                }
            }
            using (var reader = File.OpenRead(planetPath))
            {
                while (OSM.CanRead(reader))
                {
                    BlobRef blobRef = new BlobRef(reader.Position);
                    Blob blob = OSM.ReadBlob(reader);
                    List<long> blobDenseNodeStarts = blob.GetDenseNodeStarts(); // TODO: write this method
                    for (int i = 0; i < blobDenseNodeStarts.Count; i++)
                    {
                        denseNodeStarts.Add(blobDenseNodeStarts[i]);
                        blobRefs.Add(blobRef);
                        denseNodeIndexes.Add(i);
                    }
                }
            }
            using (nodeReader = File.OpenRead(planetPath))
            {
                using (var reader = File.OpenRead(planetPath))
                {
                    while (OSM.CanRead(reader))
                    {
                        Blob blob = OSM.ReadBlob(reader);
                        List<List<long>> ways = blob.GetWayIds("highway");
                        foreach (var way in ways)
                        {
                            List<LongLatPair> longLats = new List<LongLatPair>();
                            foreach (var nodeId in way)
                            {
                                longLats.Add(GetNodeLongLatInfo(nodeId));
                            }
                            long minLong = longLats.Min(x => x.longVal);
                            long maxLong = longLats.Max(x => x.longVal);
                            long minLat = longLats.Min(x => x.latVal);
                            long maxLat = longLats.Max(x => x.latVal);
                            if (maxLong - minLong > 1800000000) // probably wraps around
                            {
                                minLong = longLats.Min(x => x.longVal < 1800000000 ? x.longVal + 3600000000 : x.longVal);
                                maxLong = longLats.Max(x => x.longVal < 1800000000 ? x.longVal + 3600000000 : x.longVal);
                            }
                            // zoom is at 2^10
                            double minX = minLong / 3600000000.0 * 1024;
                            double maxX = maxLong / 3600000000.0 * 1024;
                            double minY = minLat / 1800000000.0 * 1024;
                            double maxY = maxLat / 1800000000.0 * 1024;
                            List<Sector> containedSectors = new List<Sector>();
                            for (int i = (int)Math.Floor(minX); i <= (int)Math.Ceiling(maxX); i++)
                            {
                                for (int j = (int)Math.Floor(minY); j <= (int)Math.Ceiling(maxY); j++)
                                {
                                    int x = (i + 10240) % 1024;
                                    int y = (j + 10240) % 1024;
                                    WriteLongLats(longLats, outputStreams[x, y]);
                                }
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < 1024; i++)
            {
                for (int j = 0; j < 1024; j++)
                {
                    outputStreams[i, j].Dispose();
                }
            }
        }

        private void WriteLongLats(List<LongLatPair> longLats, FileStream fileStream)
        {
            OSM.WriteVarInt(fileStream, longLats.Count);
            long lastLong = 0;
            long lastLat = 0;
            for (int i = 0; i < longLats.Count; i++)
            {
                long thisLong = longLats[i].longVal;
                long thisLat = longLats[i].latVal;
                OSM.WriteSignedVarInt(fileStream, thisLong - lastLong);
                OSM.WriteSignedVarInt(fileStream, thisLat - lastLat);
                lastLong = thisLong;
                lastLat = thisLat;
            }
        }

        private LongLatPair GetNodeLongLatInfo(long nodeId)
        {
            DenseNodes dense = GetDenseNodes(nodeId);
            int pos = dense.id.BinarySearch(nodeId);
            if (pos < 0) throw new NotImplementedException();
            return new LongLatPair(dense.lon[pos], dense.lat[pos]);
        }
        // TODO: generate these
        List<long> denseNodeStarts = new List<long>(); // the id of the first item in each denseNode
        List<BlobRef> blobRefs = new List<BlobRef>(); // a reference to the blob containing the denseNode
        List<int> denseNodeIndexes = new List<int>(); // the index of the denseNode within its blob
        Queue<BlobRef> lastAccessedBlobs = new Queue<BlobRef>(); // keeps track of last blobs used
        static int MAX_CACHE = 3;
        private DenseNodes GetDenseNodes(long nodeId)
        {
            int pos = denseNodeStarts.BinarySearch(nodeId);
            if (pos < 0) pos = (~pos) - 1;
            if (!lastAccessedBlobs.Contains(blobRefs[pos]))
            {
                blobRefs[pos].Load(nodeReader);
                if (lastAccessedBlobs.Count >= MAX_CACHE) lastAccessedBlobs.Dequeue().Unload();
                lastAccessedBlobs.Enqueue(blobRefs[pos]);
            }
            return blobRefs[pos].dense[denseNodeIndexes[pos]];
        }

        public class BlobRef
        {
            long startPos;
            public List<DenseNodes> dense;
            public BlobRef(long startPos)
            {
                this.startPos = startPos;
            }

            public void Load(FileStream nodeReader)
            {
                nodeReader.Seek(startPos, SeekOrigin.Begin);
                Blob blob = OSM.ReadBlob(nodeReader);
                dense = blob.GetDense();
            }

            public void Unload()
            {
                dense = null;
            }
        }

        public class LongLatPair
        {
            public long longVal;
            public long latVal;
            public LongLatPair(long longVal, long latVal)
            {
                this.longVal = longVal;
                this.latVal = latVal;
            }
        }
    }
}

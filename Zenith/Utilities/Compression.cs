using System;
using System.IO;
using System.IO.Compression;

namespace Zenith.Utilities
{
    class Compression
    {
        internal static byte[] UnZLibToBytes(byte[] zlib_data, int raw_size)
        {
            using (var memStream = new MemoryStream(zlib_data))
            {
                // skip first two bytes
                // "Those bytes are part of the zlib specification (RFC 1950), not the deflate specification (RFC 1951). Those bytes contain information about the compression method and flags."
                memStream.ReadByte();
                memStream.ReadByte();
                using (var deflateStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    byte[] unzipped = new byte[raw_size];
                    deflateStream.Read(unzipped, 0, raw_size);
                    return unzipped;
                }
            }
        }

        internal static byte[] UnZipToBytes(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                if (zip.Entries.Count != 1) throw new NotImplementedException();
                foreach (var entry in zip.Entries)
                {
                    using (var zipStream = entry.Open())
                    {
                        var memStream = new MemoryStream();
                        zipStream.CopyTo(memStream);
                        return memStream.ToArray();
                    }
                }
            }
            throw new NotImplementedException();
        }

        internal static void ZipToFile(string filePath, byte[] bytes)
        {
            using (FileStream compressedFileStream = File.Create(filePath))
            using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
            {
                new MemoryStream(bytes).CopyTo(compressionStream);
            }
        }
    }
}
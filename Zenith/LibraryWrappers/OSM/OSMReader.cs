using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zenith.ZMath;
#if ANDROID
using ZenithAndroid;
#endif

namespace Zenith.LibraryWrappers.OSM
{
    public class OSMReader
    {
        internal static BlobCollection GetAllBlobs(ISector sector)
        {
            List<Blob> blobs = new List<Blob>();
#if WINDOWS || LINUX
            string path = OSMPaths.GetSectorPath(sector);
            using (var reader = File.OpenRead(path))
            {
                while (true)
                {
                    Blob blob = ReadBlob(reader);
                    if (blob == null) break;
                    blob.Init();
                    blobs.Add(blob);
                }
            }
#else
            string path = OSMPaths.GetSectorPath(sector);
            using (var reader = Activity1.ASSETS.Open(path))
            {
                while (true)
                {
                    Blob blob = ReadBlob(reader);
                    if (blob == null) break;
                    blob.Init();
                    blobs.Add(blob);
                }
            }
#endif
            return new BlobCollection(blobs, sector);
        }

        internal static List<long> ReadPackedSignedVarInts(Stream stream)
        {
            long length = ReadVarInt(stream);
            long end = stream.Position + length;
            List<long> nums = new List<long>();
            while (stream.Position < end)
            {
                nums.Add(ReadSignedVarInt(stream));
            }
            if (stream.Position != end) throw new NotImplementedException();
            return nums;
        }

        public static long ReadSignedVarInt(Stream reader)
        {
            // note, these varints have the least significant byte first
            // note, the sign bit is the first byte
            int b = reader.ReadByte();
            if ((b & 1) == 1)
            {
                long value = ((b & 127) + 1) / -2;
                long move = -64;
                while ((b & 128) == 128)
                {
                    b = reader.ReadByte();
                    value += (b & 127) * move;
                    move *= 128L;
                }
                return value;
            }
            else
            {
                long value = (b & 127) / 2;
                long move = 64;
                while ((b & 128) == 128)
                {
                    b = reader.ReadByte();
                    value += (b & 127) * move;
                    move *= 128L;
                }
                return value;
            }
        }

        public static long ReadVarInt(Stream reader)
        {
            // note, these varints have the least significant byte first
            long b = reader.ReadByte();
            long value = b & 127;
            int move = 7;
            while ((b & 128) == 128)
            {
                b = reader.ReadByte();
                value += (b & 127) << move;
                move += 7;
            }
            return value;
        }

        internal static List<long> ReadPackedDeltaCodedVarInts(Stream stream)
        {
            long length = ReadVarInt(stream);
            long end = stream.Position + length;
            List<long> nums = new List<long>();
            while (stream.Position < end)
            {
                if (nums.Count == 0)
                {
                    nums.Add(ReadSignedVarInt(stream));
                }
                else
                {
                    nums.Add(nums.Last() + ReadSignedVarInt(stream));
                }
            }
            if (stream.Position != end) throw new NotImplementedException();
            return nums;
        }

        internal static List<long> ReadPackedVarInts(Stream stream)
        {
            long length = ReadVarInt(stream);
            long end = stream.Position + length;
            List<long> nums = new List<long>();
            while (stream.Position < end)
            {
                nums.Add(ReadVarInt(stream));
            }
            if (stream.Position != end) throw new NotImplementedException();
            return nums;
        }

        internal static void SkipBytes(Stream stream)
        {
            long length = ReadVarInt(stream);
            stream.Seek(length, SeekOrigin.Current);
        }

        public static Blob ReadBlob(Stream reader)
        {
            Blob blob = new Blob();
            int? length = ReadInt32UnlessEOF(reader);
            if (!length.HasValue) return null;
            blob.length = length.Value;
            byte b = (byte)reader.ReadByte();
            if (b != 10) throw new NotImplementedException();
            blob.type = ReadString(reader);
            b = (byte)reader.ReadByte();
            if (b == 18)
            {
                blob.indexdata = ReadBytes(reader);
                b = (byte)reader.ReadByte();
            }
            if (b != 24) throw new NotImplementedException();
            blob.datasize = (int)ReadVarInt(reader);
            b = (byte)reader.ReadByte();
            if (b == 16)
            {
                blob.raw_size = (int)ReadVarInt(reader);
                b = (byte)reader.ReadByte();
            }
            if (b != 26) throw new NotImplementedException();
            blob.zlib_data = ReadBytes(reader);
            return blob;
        }

        public static byte[] ReadBytes(Stream reader)
        {
            long length = ReadVarInt(reader);
            byte[] bytes = new byte[length];
            reader.Read(bytes, 0, (int)length);
            return bytes;
        }

        public static string ReadString(Stream reader)
        {
            byte[] bytes = ReadBytes(reader);
            char[] chars = new char[bytes.LongLength];
            for (long i = 0; i < bytes.LongLength; i++) chars[i] = (char)bytes[i];
            return new string(chars);
        }

        public static int ReadInt32(Stream reader)
        {
            int b1 = reader.ReadByte() << 12;
            int b2 = reader.ReadByte() << 8;
            int b3 = reader.ReadByte() << 4; // wait should this be 8, 16, 24???
            int b4 = reader.ReadByte();
            return b1 + b2 + b3 + b4;
        }

        public static int? ReadInt32UnlessEOF(Stream reader)
        {
            int b = reader.ReadByte();
            if (b < 0) return null;
            int b1 = b << 12;
            int b2 = reader.ReadByte() << 8;
            int b3 = reader.ReadByte() << 4;
            int b4 = reader.ReadByte();
            return b1 + b2 + b3 + b4;
        }
    }
}

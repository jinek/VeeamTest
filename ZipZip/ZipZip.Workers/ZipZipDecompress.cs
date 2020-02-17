using System;
using System.IO;
using System.IO.Compression;

namespace ZipZip.Workers
{
    internal sealed class ZipZipDecompress : ZipZipWorkerBase<MemoryStream, byte[]>
    {
        protected override byte[] ProcessChunk(MemoryStream chunk)
        {
            using (var gZipStream = new GZipStream(chunk, CompressionMode.Decompress))
            {
                return gZipStream.ReadAndTrancuateIfNeeded(BlockSize);
            }
        }

        protected override bool ReadChunk(Stream stream, out MemoryStream chunk)
        {
            chunk = null;
            var bytes = new byte[4];
            if (stream.Read(bytes, 0, 4) == 0) return false;
            int length = BitConverter.ToInt32(bytes, 0);
            var buffer = new byte[length];
            stream.Read(buffer, 0, length);
            chunk = new MemoryStream(buffer);
            return true;
        }

        protected override void WriteChunk(Stream stream, byte[] chunk)
        {
            stream.Write(chunk, 0, chunk.Length);
        }

        public ZipZipDecompress(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
        {
        }
    }
}
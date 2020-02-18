using System;
using System.IO;
using System.IO.Compression;
using ZipZip.Exceptions;
using ZipZip.Workers.Helpers;

namespace ZipZip.Workers.Processing
{
    /// <summary>
    ///     Decompresses data
    /// </summary>
    internal sealed class ZipZipDecompress : ZipZipWorkerBase<MemoryStream, byte[]>
    {
        public ZipZipDecompress(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
        {
        }

        /// <inheritdoc />
        protected override byte[] ProcessChunk(MemoryStream chunk)
        {
            using (var gZipStream = new GZipStream(chunk, CompressionMode.Decompress))
            {
                return gZipStream.ReadAndTruncateIfNeeded(BlockSize);
            }
        }

        /// <inheritdoc />
        protected override bool ReadChunk(Stream stream, out MemoryStream chunk)
        {
            chunk = null;
            var bytes = new byte[4];
            if (stream.Read(bytes, 0, 4) == 0) return false;
            int length = BitConverter.ToInt32(bytes, 0);

            if (length <= 0 || length > BlockSize * 2)
                UserErrorException.ThrowUserErrorException("Invalid input file format");

            var buffer = new byte[length];
            if (stream.Read(buffer, 0, length) < length)
                UserErrorException.ThrowUserErrorException("Invalid input file format. File is too short");

            chunk = new MemoryStream(buffer);

            return true;
        }

        /// <inheritdoc />
        protected override void WriteChunk(Stream stream, byte[] chunk)
        {
            stream.Write(chunk, 0, chunk.Length);
        }
    }
}
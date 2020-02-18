using System;
using System.IO;
using System.IO.Compression;
using ZipZip.Workers.Helpers;

namespace ZipZip.Workers.Processing
{
    /// <summary>
    ///     Does compression job. 
    /// </summary>
    internal sealed class ZipZipCompress : ZipZipWorkerBase<byte[], MemoryStream>
    {
        public ZipZipCompress(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
        {
        }

        /// <inheritdoc />
        protected override MemoryStream ProcessChunk(byte[] chunk)
        {
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(chunk, 0, chunk.Length);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }


        /// <inheritdoc />
        protected override bool ReadChunk(Stream inputStream, out byte[] bytes)
        {
            bytes = inputStream.ReadAndTruncateIfNeeded(BlockSize);

            return bytes.Length != 0;
        }

        /// <inheritdoc />
        protected override void WriteChunk(Stream outputStream, MemoryStream chunk)
        {
            // https://social.msdn.microsoft.com/Forums/en-US/9d15ca74-3db7-478f-8f29-3579ef7fae5c/how-to-read-a-long-with-filestream
            outputStream.Write(BitConverter.GetBytes((int) chunk.Length), 0,
                sizeof(int));

            chunk.CopyTo(outputStream);
        }
    }
}
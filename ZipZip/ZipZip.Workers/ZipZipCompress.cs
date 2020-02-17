using System;
using System.IO;
using System.IO.Compression;

namespace ZipZip.Workers
{
    internal sealed class ZipZipCompress : ZipZipWorkerBase<byte[], MemoryStream>
    {
        

        protected override MemoryStream ProcessChunk(byte[] chunk)
        {
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gZipStream.Write(chunk, 0, chunk.Length);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        protected override bool ReadChunk(Stream _inputStream, out byte[] bytes)
        {
            bytes = _inputStream.ReadAndTrancuateIfNeeded(BlockSize);
            return bytes.Length != 0;
        }

        protected override void WriteChunk(Stream _outputStream, MemoryStream chunk)
        {
            _outputStream.Write(BitConverter.GetBytes(chunk.Length), 0,
                sizeof(long)); //// https://social.msdn.microsoft.com/Forums/en-US/9d15ca74-3db7-478f-8f29-3579ef7fae5c/how-to-read-a-long-with-filestream

            chunk.CopyTo(_outputStream);
        }

        public ZipZipCompress(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
        {
        }
    }

    //todo: подставить checked для чисел
}
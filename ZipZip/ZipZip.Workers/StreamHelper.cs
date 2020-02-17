using System;
using System.IO;

namespace ZipZip.Workers
{
    internal static class StreamHelper
    {
        public static byte[] ReadAndTrancuateIfNeeded(this Stream stream, int desiredSize)
        {
            var buffer = new byte[desiredSize];
            int read = stream.Read(buffer, 0, desiredSize);
            
            if (read == desiredSize) return buffer;
            
            if (read == 0) return new byte[0];
            
            var truncuatedArray = new byte[read];
            Array.Copy(buffer, 0, truncuatedArray, 0, read);
            return truncuatedArray;
        }
    }
}
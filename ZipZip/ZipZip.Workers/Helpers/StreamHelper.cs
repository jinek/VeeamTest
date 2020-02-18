using System;
using System.IO;
using ZipZip.Exceptions;

namespace ZipZip.Workers.Helpers
{
    internal static class StreamHelper
    {
        public static byte[] ReadAndTruncateIfNeeded(this Stream stream, int desiredSize)
        {
            var buffer = new byte[desiredSize];

            int read = stream.Read(buffer, 0, desiredSize);

            if (read == desiredSize) return buffer;

            if (read == 0) return new byte[0];

            var truncatedArray = new byte[read];

            Array.Copy(buffer, 0, truncatedArray, 0, read);

            return truncatedArray;
        }

        /// <summary>
        ///     Rethrows exception to be shown to user in some specific cases
        /// </summary>
        public static void ConvertFileExceptions(this Exception exception)
        {
            switch (exception)
            {
                case IOException _:
                case UnauthorizedAccessException _:
                case InvalidDataException _: //this is gzip case 
                    //SecurityException _: //should not be thrown in normal run case:
                    
                    UserErrorException.ThrowUserErrorException($"Error processing file: {exception.Message}");
                    
                    break;
            }
        }
    }
}
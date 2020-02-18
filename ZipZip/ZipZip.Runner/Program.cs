using System.Linq;
using ZipZip.Exceptions;
using ZipZip.Workers.Processing;

namespace ZipZip.Runner
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            ExceptionManager.EnsureManagerInitialized();
            
            args = args.Where(str => !string.IsNullOrEmpty(str)).ToArray();
            
            if (args.Length != 3)
                UserErrorException.ThrowUserErrorException("There should be 3 arguments");

            bool compress = ParseMode(args[0]);


            string inputPath = args[1];
            string outputPath = args[2];

            ZipZipProcessing.Process(inputPath, outputPath, compress);
        }

        private static bool ParseMode(string argsZero)
        {
            bool compress;
            switch (argsZero?.ToUpper())
            {
                case "COMPRESS":
                    compress = true;
                    break;
                case "DECOMPRESS":
                    compress = false;
                    break;
                default:
                    throw new UserErrorException("First argument must be compress or decompress");
            }

            return compress;
        }
    }
}
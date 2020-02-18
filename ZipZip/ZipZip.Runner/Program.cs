using ZipZip.Exceptions;
using ZipZip.Workers;

namespace ZipZip.Runner
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            ExceptionManager.CatchExceptionsFromNowAndForever();

            if (args.Length != 3)
                UserErrorException.ThrowUserErrorException("There should be 3 arguments");

            bool compress = ParseMode(args[0]);


            string inputPath = args[1] ?? throw new UserErrorException("Second argument must be input file name");
            string outputPath = args[2] ?? throw new UserErrorException("Third argument must be output file name");

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
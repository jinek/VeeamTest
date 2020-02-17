using ZipZip.Workers;

namespace ZipZip.Runner
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            bool compress=true;
            string inputPath = @"C:\Temp2\9.0_Holger_shared_parts_Cost_of_change.bak";
            string outputPath = @"C:\Temp2\ZipZipResult.zz";
            ZipZipProcessing.Process(inputPath,outputPath,compress);
        }
    }
}
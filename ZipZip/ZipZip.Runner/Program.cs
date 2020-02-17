using ZipZip.Workers;

namespace ZipZip.Runner
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            bool compress = false;//true;
            string inputPath = @"C:\Temp2\ZipZipResult.zz";//@"C:\Temp2\9.0_Holger_shared_parts_Cost_of_change.bak";
            string outputPath = /*@"C:\Temp2\ZipZipResult.zz";//*/@"C:\Temp2\ZipZipResult2.zz";
            ZipZipProcessing.Process(inputPath,outputPath,compress);
        }
    }
}
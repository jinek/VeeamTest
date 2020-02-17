namespace ZipZip.Workers
{
    public static class ZipZipProcessing
    {
        public static void Process(string inputPath, string outputPath, bool compress)
        {
            using (IZipZipWorker worker = compress
                ? (IZipZipWorker) new ZipZipCompress(inputPath, outputPath)
                : new ZipZipDecompress(inputPath, outputPath))
            {
                worker.Process();
            }
        }
    }
}
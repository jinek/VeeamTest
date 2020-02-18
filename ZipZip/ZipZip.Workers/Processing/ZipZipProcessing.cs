namespace ZipZip.Workers.Processing
{
    /// <summary>
    ///     Use this class to compress or decompress data
    /// </summary>
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
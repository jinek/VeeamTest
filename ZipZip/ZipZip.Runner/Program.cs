namespace ZipZip.Runner
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            using (var zipZipWorker = new ZipZipWorker(WorkerParameters.ParseUserInput(args)))
            {
                zipZipWorker.Process();
            }
        }
    }
}
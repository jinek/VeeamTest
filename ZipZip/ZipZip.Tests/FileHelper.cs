using System.IO;
using System.Linq;

namespace ZipZip.Tests
{
    public static class FileHelper
    {
        public static void GenerateFile(int fileSize, string fileName)
        {
            File.WriteAllBytes(fileName, Enumerable.Range(0, fileSize).Select(b => (byte) b).ToArray());
        }
    }
}
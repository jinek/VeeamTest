using System.IO;
using System.Linq;
using NUnit.Framework;
using ZipZip.Workers.Processing;

namespace ZipZip.Tests
{
    [TestFixture]
    public class NormalExecutionTests
    {
        [TearDown]
        public void Cleanup()
        {
            File.Delete(SourceFileName);
            File.Delete(CompressedFileName);
            File.Delete(DecompressedFileName);
        }

        private const string SourceFileName = "TestSourceFile.txt";
        private const string CompressedFileName = "compressedFile.txt";
        private const string DecompressedFileName = "decompressedFile.txt";

        [Test]
        [TestCase(100 * 1000000, Description = "Small file")]
        [TestCase(1, Description = "1 byte file")]
        [TestCase(0, Description = "Empty file")]
        public void NormalExecutionTest(int fileSize)
        {
            FileHelper.GenerateFile(fileSize, SourceFileName);

            ZipZipProcessing.Process(SourceFileName, CompressedFileName, true);
            Assert.True(File.Exists(CompressedFileName));
            ZipZipProcessing.Process(CompressedFileName, DecompressedFileName, false);
            Assert.True(File.Exists(DecompressedFileName));
            Assert.True(File.ReadAllBytes(DecompressedFileName).SequenceEqual(File.ReadAllBytes(SourceFileName)));
        }
    }
}
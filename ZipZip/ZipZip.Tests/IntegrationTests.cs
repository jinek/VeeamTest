using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using ZipZip.Runner;

namespace ZipZip.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [SetUp]
        public void Init()
        {
            FileHelper.GenerateFile(1000, _fileName);
        }

        [TearDown]
        public void Cleanup()
        {
            File.Delete(_fileName);
        }

        private static readonly string MainApplicationPath = typeof(ReferenceForAssembly).Assembly.Location;
        private readonly string _fileName = Path.Combine(Path.GetDirectoryName(MainApplicationPath), "testFile.txt");

        private readonly string _outputFilePath =
            Path.Combine(Path.GetDirectoryName(MainApplicationPath), "output.txt");

        [TestCase]
        public void TestOk()
        {
            int processExitCode = RunProcess(_fileName, _outputFilePath);
            Assert.AreEqual(0, processExitCode);
        }

        [TestCase]
        public void TestWrongFormat()
        {
            int processExitCode = RunProcess(_fileName, _outputFilePath, "decompress");
            Assert.AreEqual(1, processExitCode);
        }

        [TestCase]
        public void TestFileBusy()
        {
            using (File.OpenWrite(_fileName))
            {
                int processExitCode = RunProcess(_fileName, _outputFilePath);
                Assert.AreEqual(1, processExitCode);
            }
        }

        [TestCase]
        public void TestWrongInputFile()
        {
            int processExitCode = RunProcess("C:\\A2949483-4B08-48B6-90F4-5446111F7638.txt", _outputFilePath);
            Assert.AreEqual(1, processExitCode);
        }

        [TestCase]
        public void TestWrongOutputFile()
        {
            int processExitCode = RunProcess(_fileName, "C:\\A2949483-4B08-48B6-90F4-5446111F7638\\test.txt");
            Assert.AreEqual(1, processExitCode);
        }

        [TestCase]
        public void TestWrongCommand()
        {
            int processExitCode = RunProcess(_fileName, _outputFilePath, "beautify");
            Assert.AreEqual(1, processExitCode);
        }

        [TestCase]
        public void TestWrongParameters()
        {
            int processExitCode = RunProcess(_fileName, _outputFilePath, string.Empty);
            Assert.AreEqual(1, processExitCode);
        }

        [TestCase]
        public void TestNoParameters()
        {
            int processExitCode = RunProcess(string.Empty, string.Empty, string.Empty);
            Assert.AreEqual(1, processExitCode);
        }

        private static int RunProcess(string inputFile, string outputFile, string command = "compress")
        {
            Process process = Process.Start(new ProcessStartInfo(MainApplicationPath,
                $@"{command} ""{inputFile}"" ""{outputFile}""")
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            });

            const int milliseconds = 100000;
            if (!process.WaitForExit(milliseconds))
                Assert.Fail($"Process has not finished in {milliseconds} milliseconds");

            int processExitCode = process.ExitCode;
            return processExitCode;
        }
    }
}
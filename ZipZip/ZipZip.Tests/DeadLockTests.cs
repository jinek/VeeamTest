using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ZipZip.Workers.Processing;

namespace ZipZip.Tests
{
    [TestFixture]
    public class DeadLockTests
    {
        [SetUp]
        public void Init()
        {
            FileHelper.GenerateFile(0, InputFilePath);
        }

        [TearDown]
        public void Cleanup()
        {
            File.Delete(InputFilePath);
            File.Delete(OutputFilePath);
        }

        private const string InputFilePath = "input.txt";
        private const string OutputFilePath = "output.txt";

        private class DeadLockAttemptWorker_10MS_OutputDelay : ZipZipWorkerBase<object, object>
        {
            private readonly int _inputChunksNumber;
            private readonly bool _outputOverflow;
            private int _inputIndex;

            public DeadLockAttemptWorker_10MS_OutputDelay(string inputFilePath, string outputFilePath,
                bool outputOverflow, int inputChunksNumber) : base(inputFilePath, outputFilePath)
            {
                _outputOverflow = outputOverflow;
                _inputChunksNumber = inputChunksNumber;
            }

            protected override object ProcessChunk(object chunk)
            {
                return null;
            }

            protected override bool ReadChunk(Stream stream, out object chunk)
            {
                if (!_outputOverflow)
                    Thread.Sleep(10);
                chunk = null;
                return _inputIndex++ != _inputChunksNumber;
            }

            protected override void WriteChunk(Stream stream, object chunk)
            {
                if (_outputOverflow)
                    Thread.Sleep(10);
            }
        }

        [Test]
        //20 seconds test
        [TestCase(true, Description = "DeadLock while output buffer overflow")]
        [TestCase(false, Description = "DeadLock while output buffer overflow")]
        public void TestByBufferOverlow(bool inputOrOutputBufferOverflow)
        {
            int inputChunksNumber = 1000;
            Task task = Task.Run(() =>
            {
                using (var deadLockTester = new DeadLockAttemptWorker_10MS_OutputDelay(InputFilePath, OutputFilePath,
                    inputOrOutputBufferOverflow, inputChunksNumber))
                {
                    deadLockTester.Process();
                }
            });

            bool waitedFine;
            if (Debugger.IsAttached)
            {
                task.Wait();
                waitedFine = true;
            }
            else
            {
                waitedFine = task.Wait(TimeSpan.FromMilliseconds(10 * inputChunksNumber * 2));
            }

            Assert.True(waitedFine,
                "has not finished in time. Chance of dead lock here");
        }
    }
}
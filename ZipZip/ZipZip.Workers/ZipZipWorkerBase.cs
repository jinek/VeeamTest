using System;
using System.IO;
using ZipZip.Lockers;

namespace ZipZip.Workers
{
    internal abstract class ZipZipWorkerBase<TInput,TOutput> : IZipZipWorker
    {
        private readonly AccessBlockingDataPool<TInput> _inputBuffer;
        private readonly FileStream _inputStream;

        private readonly AccessBlockingDataPool<TOutput> _outputBuffer; 

        private readonly FileStream _outputStream;

        private int _finishBlock = -1;

        private readonly ThreadManager _threadManager = new ThreadManager();


        protected ZipZipWorkerBase(string inputFilePath,string outputFilePath)
        {
            _inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
            
            _outputStream = new FileStream(outputFilePath, FileMode.Create);
            
            _inputBuffer = new AccessBlockingDataPool<TInput>(MaxWorkerThreads * 3,false);
            _outputBuffer = new AccessBlockingDataPool<TOutput>(MaxWorkerThreads * 3, true);
        }

        protected const int BlockSize = 1024*1024;

        private static int MaxWorkerThreads => Environment.ProcessorCount;

        public void Dispose()
        {
            _threadManager.Dispose();
            _inputStream.Dispose();
            _outputStream.Dispose();
        }

        public void Process()
        {
            RunReadInputWorker();
            RunZippingWorkers();
            WriteOutput();
        }

        private void RunReadInputWorker()
        {
            _threadManager.RunThread(() =>
            {
                var order = 0;
                while (ReadChunk(_inputStream, out TInput chunk)) _inputBuffer.Add(chunk, order++);

                _finishBlock = order;
            });
        }

        protected abstract TOutput ProcessChunk(TInput chunk);

        private void RunZippingWorkers()
        {
            for (var i = 0; i < MaxWorkerThreads; i++)
                _threadManager.RunThread(() =>
                {
                    TInput chunk = _inputBuffer.Pop(out int order);

                    TOutput processedChunk = ProcessChunk(chunk);

                    _outputBuffer.Add(processedChunk, order);
                });
        }

        private void WriteOutput()
        {
                var order = 0;
                while (order!=_finishBlock)
                {
                    TOutput chunk = _outputBuffer.Pop(order++);
                    WriteChunk(_outputStream,chunk);
                }
        }

        protected abstract bool ReadChunk(Stream stream,out TInput chunk);

        protected abstract void WriteChunk(Stream stream, TOutput chunk);
    }
}
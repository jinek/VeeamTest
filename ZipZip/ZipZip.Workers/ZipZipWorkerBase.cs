using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            
            _inputBuffer = new AccessBlockingDataPool<TInput>(BufferSize,false);
            _outputBuffer = new AccessBlockingDataPool<TOutput>(BufferSize, true);
        }

        private static int BufferSize => MaxWorkerThreads * BufferSizeFromCPUNumberMultiplier;

        protected const int BlockSize = 1024*1024;
        private const int BufferSizeFromCPUNumberMultiplier = 3;

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
                    while (true)//todo: будет оборвано дислоузом
                    {
                        TInput chunk = _inputBuffer.Pop(out int order);

                        WritePool();
                        TOutput processedChunk = ProcessChunk(chunk);

                        _outputBuffer.Add(processedChunk, order);
                    }
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
                //todo: надо дождаться завершения
        }
        
        private void WritePool()
        {
            int outputCount = _outputBuffer._bag.Count;
            int inputCount = _inputBuffer._bag.Count;

            Debug.WriteLine(string.Concat(Enumerable.Repeat('|', inputCount)).PadRight(BufferSize,'.') +
                            "            " + string.Concat(Enumerable.Repeat('|', outputCount))
                                .PadRight(BufferSize,'.'));
        }

        protected abstract bool ReadChunk(Stream stream,out TInput chunk);

        protected abstract void WriteChunk(Stream stream, TOutput chunk);
    }
}
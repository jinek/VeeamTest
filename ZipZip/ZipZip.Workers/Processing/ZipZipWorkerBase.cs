using System;
using System.IO;
using ZipZip.Exceptions;
using ZipZip.Threading;
using ZipZip.Workers.DataBuffer;
using ZipZip.Workers.Helpers;

namespace ZipZip.Workers.Processing
{
    /// <summary>
    ///     Not thread safe!
    ///     This is base class which can be used to read some data, process it in several threads and write some data.
    ///     There are two buffers between this three processes: <see cref="_inputBuffer"/> and <see cref="_outputBuffer"/>
    ///     For general idea see <see cref="Process"/>
    ///
    ///     Generic parameters are used for optimization (less data copies)
    /// </summary>
    internal abstract class ZipZipWorkerBase<TInput, TOutput> : IZipZipWorker
    {
        protected const int BlockSize = 1024 * 1024;
        private const int BufferSizeFromCPUNumberMultiplier = 3;
        private readonly AccessBlockingDataBuffer<TInput> _inputBuffer;
        private readonly FileStream _inputStream;
        private readonly AccessBlockingDataBuffer<TOutput> _outputBuffer;
        private readonly FileStream _outputStream;
        private readonly ThreadManager<ProcessingFinishedException> _threadManager = new ThreadManager<ProcessingFinishedException>();
        private volatile int _finishBlock = -1;

        protected ZipZipWorkerBase(string inputFilePath, string outputFilePath)
        {
            try
            {
                _inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                _outputStream = new FileStream(outputFilePath, FileMode.Create);
            }
            catch (Exception exception)
            {
                exception.ConvertFileExceptions();
                throw;
            }

            _inputBuffer = new AccessBlockingDataBuffer<TInput>(BufferSize, false);
            _outputBuffer = new AccessBlockingDataBuffer<TOutput>(BufferSize, true);
        }

        private static int BufferSize => MaxWorkerThreads * BufferSizeFromCPUNumberMultiplier;
        private static int MaxWorkerThreads => Environment.ProcessorCount;

        public void Dispose()
        {
            //its not real dispose. It must be called from same Thread
            _threadManager.WaitAllToFinish();
            _inputStream.Dispose();
            _outputStream.Dispose();
        }

        public void Process()
        {
            RunZippingWorkers();
            RunWriteOutputWorker();
            RunReading();
        }

        private void RunReading()
        {
            int order = 0;
            try
            {
                while (ReadChunk(_inputStream, out TInput chunk))
                    _inputBuffer.Add(chunk, order++);
            }
            catch (Exception exception)
            {
                exception.ConvertFileExceptions();
                throw;
            }

            _finishBlock = order;
            
            _threadManager.WaitAllToFinish();
        }

        /// <summary>
        ///     Data (chunk) transformation
        /// </summary>
        protected abstract TOutput ProcessChunk(TInput chunk);

        private void RunZippingWorkers()
        {
            for (int i = 0; i < MaxWorkerThreads; i++)
                _threadManager.RunThread(() =>
                {
                    while (true)
                    {
                        TInput chunk = _inputBuffer.Pull(out int order);

                        TOutput processedChunk;
                        try
                        {
                            processedChunk = ProcessChunk(chunk);
                        }
                        catch (Exception exception)
                        {
                            exception.ConvertFileExceptions();
                            throw;
                        }

                        _outputBuffer.Add(processedChunk, order);
                    }
                });
        }

        private void RunWriteOutputWorker()
        {
            _threadManager.RunThread(() =>
            {
                int order = 0;
                while (_finishBlock == -1 || order < _finishBlock)
                {
                    TOutput chunk = _outputBuffer.Pull(order++);

                    try
                    {
                        WriteChunk(_outputStream, chunk);
                    }
                    catch (Exception exception)
                    {
                        exception.ConvertFileExceptions();
                        throw;
                    }
                }

                _inputBuffer.AbortAllWaiters();
                _outputBuffer.AbortAllWaiters();
            });
        }

        /// <summary>
        ///     Reading chunk from input file
        /// </summary>
        protected abstract bool ReadChunk(Stream stream, out TInput chunk);

        /// <summary>
        ///     Put processed (converted) data in to output file
        /// </summary>
        protected abstract void WriteChunk(Stream stream, TOutput chunk);
    }
}
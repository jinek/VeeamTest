using System;
using System.IO;
using System.IO.Compression;

namespace ZipZip.Workers
{
    public class ZipZipWorker : IDisposable
    {
        private readonly AccessBlockingDataPool<byte[]> _inputBuffer;
        private readonly FileStream _inputStream;
        private readonly AccessBlockingDataPool<MemoryStream> _outputBuffer;//todo: указать, что ищем поток по order, и не любой чанк
        private readonly FileStream _outputStream;
        private readonly WorkerParameters _workerParameters;
        private bool _finished;
        private ThreadManager _threadManager;
//todo: у этого класса должны быть два наследника: что б переопределили как процессить данные
        public ZipZipWorker(WorkerParameters workerParameters)
        {
            _workerParameters = workerParameters;
            //todo: initialize everything
            throw new NotImplementedException();
        }

        private int BlockSize => _workerParameters.BlockSize;

        private static int MaxWorkerThreads => Environment.ProcessorCount;

        public void Dispose()
        {
            throw new NotImplementedException("Finalizer");
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
                int order = 0;
                while (ReadChunk(out byte[] chunk)) _inputBuffer.Add(chunk, order++);

                throw new NotImplementedException("Добавить завершение");
            });
        }

        private void RunZippingWorkers()
        {
            for (int i = 0; i < MaxWorkerThreads; i++)
                _threadManager.RunThread(() =>
                {
                    int order = null;
                    byte[] chunk = _inputBuffer.Pop(out order);
                    MemoryStream memoryStream = new MemoryStream(BlockSize);
                    using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gZipStream.Write(chunk, 0, chunk.Length);
                    }

                    _outputBuffer.Add(memoryStream, order);
                });
        }
        
        //todo: потом продумать как при записи что бы относительно свободно можно было записывать в любом порядке

        private void WriteOutput()
        {
            _threadManager.RunThread(() =>
            {
                int? order = 0;
                while (!_finished)
                {
                    MemoryStream chunk = _outputBuffer.Pop(ref order);//todo: здесь указываем текущий поток для освобождения, при добавлении чанка с этим order
                    WriteChunk(chunk);
                }
            });
        }

        private bool ReadChunk(out byte[] bytes)
        {
            bytes = new byte[BlockSize];
            int read = _inputStream.Read(bytes, 0, BlockSize);
            if (read == 0)
                return false;

            if (read < BlockSize) Array.Copy(bytes, 0, bytes, 0, read);

            return true;
        }

        private void WriteChunk(MemoryStream chunk)
        {
            chunk.CopyTo(_outputStream);
        }
    }
}
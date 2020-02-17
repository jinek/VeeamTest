using System;

namespace ZipZip.Workers
{
    internal interface IZipZipWorker : IDisposable
    {
        void Process();
    }
}
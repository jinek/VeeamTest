using System;

namespace ZipZip.Workers
{
    /// <summary>
    ///     This interface is needed because base class is generic and we need access to Process and dispose methods
    /// </summary>
    internal interface IZipZipWorker : IDisposable
    {
        void Process();
    }
}
using System;

namespace ZipZip.Lockers
{
    public class ThreadManager : IDisposable
    {
        public void RunThread(Action action)
        {
            throw new NotImplementedException();
        }

        private void ReleaseUnmanagedResources()
        {
            throw new NotImplementedException("Тут остановить потоки");
            //todo: подумать ещё нужно ли холдить какие-то из потоков, когда инпут/аутпут читает
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ThreadManager()
        {
            ReleaseUnmanagedResources();
        }
    }
}
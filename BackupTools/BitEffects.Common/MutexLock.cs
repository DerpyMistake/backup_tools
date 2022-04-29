using System;

namespace BitEffects
{
    public class MutexLock : IDisposable
    {
        System.Threading.Mutex mutex;

        public MutexLock(System.Threading.Mutex mutex)
        {
            this.mutex = mutex;
            this.mutex.WaitOne();
        }

        public void Dispose()
        {
            this.mutex.ReleaseMutex();
        }
    }
}

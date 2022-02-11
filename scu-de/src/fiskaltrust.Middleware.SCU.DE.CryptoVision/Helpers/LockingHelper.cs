using System;
using System.Threading;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers
{
    public static class LockingHelper
    {
        public const int TIMEOUT_60_SECONDS = 60_000;

        public static async Task<T> PerformWithLock<T>(SemaphoreSlim semaphore, Func<Task<T>> method, int timeout = TIMEOUT_60_SECONDS)
        {
            var isLocked = false;
            try
            {
                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception(string.Format("Unable to perform lock after {0} ms", timeout));
                }
                return await method();
            }
            finally
            {
                if (isLocked)
                {
                    semaphore.Release();
                }
            }
        }

        public static async Task<T> PerformWithLock<T>(SemaphoreSlim semaphore, Func<T> method, int timeout = TIMEOUT_60_SECONDS)
        {
            var isLocked = false;
            try
            {
                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout} ms");
                }
                return method();
            }
            finally
            {
                if (isLocked)
                {
                    semaphore.Release();
                }
            }
        }

        public static async Task PerformWithLockAsync(SemaphoreSlim semaphore, Func<Task> method, int timeout = TIMEOUT_60_SECONDS)
        {
            var isLocked = false;
            try
            {
                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout} ms");
                }
                await method();
            }
            finally
            {
                if (isLocked)
                {
                    semaphore.Release();
                }
            }
        }

        public static void PerformWithLock(SemaphoreSlim semaphore, Action method, int timeout = TIMEOUT_60_SECONDS)
        {
            var isLocked = false;
            try
            {
                isLocked = semaphore.Wait(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout} ms");
                }
                method();
            }
            finally
            {
                if (isLocked)
                {
                    semaphore.Release();
                }
            }
        }

        public static async Task PerformWithLockAsync(SemaphoreSlim semaphore, Action method, int timeout = TIMEOUT_60_SECONDS)
        {
            var isLocked = false;
            try
            {
                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout} ms");
                }
                method();
            }
            finally
            {
                if (isLocked)
                {
                    semaphore.Release();
                }
            }
        }
    }
}

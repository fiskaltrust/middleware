namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Helpers
{
    public class LockHelper
    {
        private readonly ATrustSmartcardSCUConfiguration _configuration;

        public LockHelper(ATrustSmartcardSCUConfiguration configuration) => _configuration = configuration;

        public T ExecuteReaderCommandLocked<T>(int readerIndex, Func<T> func)
        {
            using var semaphore = new Semaphore(1, 1, GetLockName(readerIndex));

            var locked = false;
            try
            {
                try
                {
                    locked = semaphore.WaitOne(_configuration.ReaderTimeoutMs);
                }
                catch (AbandonedMutexException)
                {
                    locked = true;
                }

                if (!locked)
                {
                    throw new TimeoutException($"Reader {readerIndex} lock timed out after {_configuration.ReaderTimeoutMs / 1000} seconds.");
                }
                return func();
            }
            finally
            {
                if (locked)
                {
                    semaphore.Release();
                }
            }
        }

        public void ExecuteReaderCommandLocked(int readerIndex, Action action)
        {
            using var semaphore = new Semaphore(1, 1, GetLockName(readerIndex));

            var locked = false;
            try
            {
                try
                {
                    locked = semaphore.WaitOne(_configuration.ReaderTimeoutMs);
                }
                catch (AbandonedMutexException)
                {
                    locked = true;
                }

                if (!locked)
                {
                    throw new TimeoutException($"Reader {readerIndex} lock timed out after {_configuration.ReaderTimeoutMs / 1000} seconds.");
                }
                action();
            }
            finally
            {
                if (locked)
                {
                    semaphore.Release();
                }
            }
        }

        private string GetLockName(int readerIndex) => $"Global\\fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Reader{readerIndex}";
    }
}

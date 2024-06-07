namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Helpers
{
    public class LockHelper
    {
        private readonly ATrustSmartcardSCUConfiguration _configuration;

        public LockHelper(ATrustSmartcardSCUConfiguration configuration) => _configuration = configuration;

        public T ExecuteReaderCommandLocked<T>(int readerIndex, string operation, Func<T> func)
        {
            using var mutex = new Mutex(false, GetLockName(readerIndex, operation));

            var locked = false;
            try
            {
                try
                {
                    locked = mutex.WaitOne(_configuration.ReaderTimeoutMs);
                }
                catch (AbandonedMutexException)
                {
                    locked = true;
                }

                if (!locked)
                {
                    throw new TimeoutException(GetTimeoutMessage(readerIndex, operation));
                }
                return func();
            }
            finally
            {
                if (locked)
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public void ExecuteReaderCommandLocked(int readerIndex, string operation, Action action)
        {
            using var mutex = new Mutex(false, GetLockName(readerIndex, operation));

            var locked = false;
            try
            {
                try
                {
                    locked = mutex.WaitOne(_configuration.ReaderTimeoutMs);
                }
                catch (AbandonedMutexException)
                {
                    locked = true;
                }

                if (!locked)
                {
                    throw new TimeoutException(GetTimeoutMessage(readerIndex, operation));
                }
                action();
            }
            finally
            {
                if (locked)
                {
                    mutex.ReleaseMutex();
                }
            }
        }
        private string GetLockName(int readerIndex, string operation)
        {
            if (operation == Operation.InitalizeReader || operation == Operation.WaitUntilReaderIsAvailable)
            {
                return $"Global\\fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.InitalizeReader";
            }
            return $"Global\\fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Reader{readerIndex}";
        }

        private string GetTimeoutMessage(int readerIndex, string operation)
        {
            if (operation == Operation.InitalizeReader || operation == Operation.InitalizeCard || operation == Operation.WaitUntilReaderIsAvailable)
            {
                return $"{operation} lock timed out after {_configuration.ReaderTimeoutMs / 1000} seconds.";
            }
            return $"Reader {readerIndex} lock timed out after {_configuration.ReaderTimeoutMs / 1000} seconds.";

        }
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers
{
    public class LockingHelper
    {
        public const int TIMEOUT_60_SECONDS = 60_000;

        private readonly ILogger<LockingHelper> _logger;

        public LockingHelper(ILogger<LockingHelper> logger)
        {
            _logger = logger;
        }

        public async Task<T> PerformWithLock<T>(SemaphoreSlim semaphore, Func<Task<T>> method, int timeout = TIMEOUT_60_SECONDS, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var isLocked = false;
            try
            {
                _logger.LogTrace("Trying to obtain lock with timeout of {LockTimeoutMs} ms, called from {CallerMemberName}:{CallerLineNumber}.", timeout, memberName, sourceLineNumber);

                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout}ms for {memberName}:{sourceLineNumber}.");
                }

                _logger.LogTrace("Succesfully obtained lock for {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);

                return await method();
            }
            finally
            {
                if (isLocked)
                {
                    _logger.LogTrace("Releasing lock for {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);
                    semaphore.Release();
                }
            }
        }

        public async Task<T> PerformWithLock<T>(SemaphoreSlim semaphore, Func<T> method, int timeout = TIMEOUT_60_SECONDS, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var isLocked = false;
            try
            {
                _logger.LogTrace("Trying to obtain lock with timeout of {LockTimeoutMs} ms, called from {CallerMemberName}:{CallerLineNumber}.", timeout, memberName, sourceLineNumber);

                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout}ms for {memberName}:{sourceLineNumber}.");
                }

                _logger.LogTrace("Succesfully obtained lock for {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);

                return method();
            }
            finally
            {
                if (isLocked)
                {
                    _logger.LogTrace("Releasing lock for {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);
                    semaphore.Release();
                }
            }
        }

        public async Task PerformWithLock(SemaphoreSlim semaphore, Func<Task> method, int timeout = TIMEOUT_60_SECONDS, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var isLocked = false;
            try
            {
                _logger.LogTrace("Trying to obtain lock with timeout of {LockTimeoutMs} ms, called from {CallerMemberName}:{CallerLineNumber}.", timeout, memberName, sourceLineNumber);

                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout}ms for {memberName}:{sourceLineNumber}.");
                }

                _logger.LogTrace("Succesfully obtained lock.");

                await method();
            }
            finally
            {
                if (isLocked)
                {
                    _logger.LogTrace("Releasing lock for {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);
                    semaphore.Release();
                }
            }
        }

        public async Task PerformWithLock(SemaphoreSlim semaphore, Action method, int timeout = TIMEOUT_60_SECONDS, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var isLocked = false;
            try
            {
                _logger.LogTrace("Trying to obtain lock with timeout of {LockTimeoutMs} ms, called from {CallerMemberName}:{CallerLineNumber}.", timeout, memberName, sourceLineNumber);

                isLocked = await semaphore.WaitAsync(timeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {timeout}ms for {memberName}:{sourceLineNumber}.");
                }
                method();
            }
            finally
            {
                if (isLocked)
                {
                    _logger.LogTrace("Releasing lock for {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);
                    semaphore.Release();
                }
            }
        }
    }
}

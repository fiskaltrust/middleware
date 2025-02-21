using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Common.RetryLogic
{
    public interface IRetryPolicyHandler<T>
    {
        Task<K> RetryFuncAsync<K>(Func<T, Task<K>> action);
        Task RetryFuncAsync(Func<T, Task> action);
    }
}
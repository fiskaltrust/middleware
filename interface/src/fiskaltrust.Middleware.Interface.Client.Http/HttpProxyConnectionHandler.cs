using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Http
{
    internal class HttpProxyConnectionHandler<T> : IProxyConnectionHandler<T> where T : class
    {
        private readonly T _proxy;

        public HttpProxyConnectionHandler(T proxy)
        {
            _proxy = proxy;
        }

        public Task ForceReconnectAsync() => Task.CompletedTask;

        public Task ReconnectAsync() => Task.CompletedTask;

        public async Task<T> GetProxyAsync() => await Task.FromResult(_proxy);
    }
}

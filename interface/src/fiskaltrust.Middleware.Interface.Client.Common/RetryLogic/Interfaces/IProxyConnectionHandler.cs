using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Common.RetryLogic
{
    public interface IProxyConnectionHandler<T> where T : class
    {
        Task ReconnectAsync();

        Task ForceReconnectAsync();

        Task<T> GetProxyAsync();
    }
}

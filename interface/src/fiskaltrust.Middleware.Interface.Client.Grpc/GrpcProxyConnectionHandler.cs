#if NET6_0_OR_GREATER

using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using ProtoBuf.Grpc.Client;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    internal class GrpcProxyConnectionHandler<T> : IProxyConnectionHandler<T> where T : class
    {
        private T _proxy;
        private GrpcChannel _channel;
        private readonly GrpcClientOptions _options;

        public GrpcProxyConnectionHandler(GrpcClientOptions options)
        {
            _options = options;
        }

        public Task ReconnectAsync()
        {
            if (_proxy != null)
            {
                return Task.CompletedTask;
            }

            GrpcClientFactory.AllowUnencryptedHttp2 = _options.AllowUnencryptedHttp2;
            _channel = GrpcChannel.ForAddress(_options.Url, _options.ChannelOptions);
            _proxy = _channel.CreateGrpcService<T>();

            return Task.CompletedTask;
        }

        public async Task ForceReconnectAsync()
        {
            try
            {
                if (_channel != null)
                {
                    await _channel.ShutdownAsync();
                }
            }
            catch
            {
                // We can ignore the case when shutdown failed
            }
            GrpcClientFactory.AllowUnencryptedHttp2 = _options.AllowUnencryptedHttp2;
            _channel = GrpcChannel.ForAddress(_options.Url, _options.ChannelOptions);
            _proxy = _channel.CreateGrpcService<T>();
        }

        public async Task<T> GetProxyAsync()
        {
            if (_proxy == null)
            {
                await ReconnectAsync();
            }

            return _proxy;
        }
    }
}

#endif
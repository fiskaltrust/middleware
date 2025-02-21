#if !NET6_0_OR_GREATER

using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using Grpc.Core;
using ProtoBuf.Grpc.Client;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Grpc
{
    internal class NativeGrpcProxyConnectionHandler<T> : IProxyConnectionHandler<T> where T : class
    {
        private T _proxy;
        private Channel _channel;
        private readonly GrpcClientOptions _options;

        public NativeGrpcProxyConnectionHandler(GrpcClientOptions options)
        {
            _options = options;
        }

        public Task ReconnectAsync()
        {
            if (_proxy != null && _channel.State == ChannelState.Ready)
            {
                return Task.CompletedTask;
            }
            _channel = new Channel(_options.Url.Host, _options.Url.Port, _options.ChannelCredentials, _options.ChannelOptions);
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
            _channel = new Channel(_options.Url.Host, _options.Url.Port, _options.ChannelCredentials, _options.ChannelOptions);
            _proxy = _channel.CreateGrpcService<T>();
        }

        public async Task<T> GetProxyAsync()
        {
            if (_proxy == null || _channel.State != ChannelState.Ready)
            {
                await ReconnectAsync();
            }

            return _proxy;
        }
    }
}

#endif
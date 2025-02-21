using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using fiskaltrust.Middleware.Interface.Client.Soap.Extensions;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Soap
{
    internal class SoapProxyConnectionHandler<T> : IProxyConnectionHandler<T> where T : class
    {
        private T _proxy;
        private readonly SoapClientOptions _options;

        public SoapProxyConnectionHandler(SoapClientOptions options)
        {
            _options = options;
        }

        public Task ReconnectAsync()
        {
            if (_proxy != null && (_proxy as ICommunicationObject).State == CommunicationState.Opened)
            {
                return Task.CompletedTask;
            }

            var binding = ConfigureBinding();

            var factory = new ChannelFactory<T>(binding, new EndpointAddress(_options.Url));
            _proxy = factory.CreateChannel();

            return Task.CompletedTask;
        }

        public Task ForceReconnectAsync()
        {
            try
            {
                CloseWcfConnection();
            }
            catch
            {
                // We can ignore the case when shutdown failed
            }
            var binding = ConfigureBinding();
            var factory = new ChannelFactory<T>(binding, new EndpointAddress(_options.Url));
            _proxy = factory.CreateChannel();

            return Task.CompletedTask;
        }

        private void CloseWcfConnection()
        {
            if (_proxy != null)
            {
                try
                {
                    if ((_proxy as ICommunicationObject).State == CommunicationState.Opened)
                    {
                        (_proxy as ICommunicationObject).Close();
                    }
                }
                finally
                {
                    if ((_proxy as ICommunicationObject).State != CommunicationState.Closed)
                    {
                        (_proxy as ICommunicationObject).Abort();
                    }
                }
            }
        }

        private Binding ConfigureBinding()
        {
            // Use timeout * 2 to make sure the call doesn't end before the outer timeout
            var sendTimeout = _options.RetryPolicyOptions?.ClientTimeout.Double() ?? RetryPolicyOptions.Default.ClientTimeout.Double();

            return _options.Url.Scheme switch
            {
                "http" => new BasicHttpBinding(BasicHttpSecurityMode.None) { MaxReceivedMessageSize = _options.MaxReceivedMessageSize, SendTimeout = sendTimeout, ReceiveTimeout = _options.ReceiveTimeout },
                "https" => new BasicHttpBinding(BasicHttpSecurityMode.Transport) { MaxReceivedMessageSize = _options.MaxReceivedMessageSize, SendTimeout = sendTimeout, ReceiveTimeout = _options.ReceiveTimeout },
                "net.tcp" => new NetTcpBinding(SecurityMode.None) { MaxReceivedMessageSize = _options.MaxReceivedMessageSize, SendTimeout = sendTimeout, ReceiveTimeout = _options.ReceiveTimeout },

#if NET6_0_OR_GREATER
                "net.pipe" => throw new NotImplementedException("Named pipes are not supported anymore by .NET6+. Please either use an HTTP or NetTcp binding, or migrate to a more modern protocol like gRPC."),
#else
                "net.pipe" => new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { MaxReceivedMessageSize = _options.MaxReceivedMessageSize, SendTimeout = sendTimeout, ReceiveTimeout = _options.ReceiveTimeout },
#endif
                _ => throw new ArgumentException($"The url {_options.Url} is not supported.", nameof(_options.Url))
            };
        }

        public async Task<T> GetProxyAsync()
        {
            await ReconnectAsync();
            return await Task.FromResult(_proxy);
        }
    }
}

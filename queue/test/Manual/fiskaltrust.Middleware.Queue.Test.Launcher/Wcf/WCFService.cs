using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Wcf
{
    public class WCFService : IDisposable
    {
        private ServiceHost _host = null;
        private long _messageSize = 16 * 1024 * 1024;
        private TimeSpan _sendTimeout = TimeSpan.FromSeconds(15);
        private TimeSpan _receiveTimeout = TimeSpan.FromDays(20);
        private readonly ILogger<WCFService> _logger;

        public WCFService(ILogger<WCFService> logger)
        {
            _logger = logger;
        }

        private void ParseConfiguration(PackageConfiguration config)
        {
            if (config.Configuration.ContainsKey("messagesize"))
            {
                long.TryParse(config.Configuration["messagesize"].ToString(), out var messageSize);
                _messageSize = messageSize;
            }

            if (config.Configuration.ContainsKey("timeout"))
            {
                int.TryParse(config.Configuration["timeout"].ToString(), out var timeout);
                _sendTimeout = TimeSpan.FromSeconds(timeout);
            }
        }

        public void ConfigureService(PackageConfiguration config, Type type, object instance, string uri)
        {
            ParseConfiguration(config);
            try
            {
                ConfigureServiceBindings(config.Id, type, instance, uri);
                var serviceBehaviour = _host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
                if (serviceBehaviour != null)
                {
                    serviceBehaviour.InstanceContextMode = InstanceContextMode.Single;
                }
                _host.Open();
                _logger.LogInformation("{Protocol}: {PackageName} ({PackageVersion}) - Endpoint: {EndpointUrl}", "WCF Service", config.Package, config.Version, uri);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Failed to start {Protocol} {PackageName} ({PackageVersion}) - Endpoint: {EndpointUrl}", "WCF Service", config.Package, config.Version, uri);
            }
        }

        private void ConfigureServiceBindings(Guid id, Type type, object instance, string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                var defaultUrl = $"net.pipe://localhost/{id}";
                _host = new ServiceHost(instance, new Uri(defaultUrl));
                _host.AddServiceEndpoint(type, CreateNetNamedPipeBinding(), defaultUrl);

            }
            else
            {
                var baseAddress = new Uri(uri);
                _host = new ServiceHost(instance, baseAddress);
                switch (baseAddress.Scheme)
                {
                    case "http":
                        _host.AddServiceEndpoint(type, CreateBasicHttpBinding(BasicHttpSecurityMode.None), uri);
                        break;
                    case "https":
                        _host.AddServiceEndpoint(type, CreateBasicHttpBinding(BasicHttpSecurityMode.Transport), uri);
                        break;
                    case "net.tcp":
                        _host.AddServiceEndpoint(type, CreateNetTcpBinding(), uri);
                        break;
                    case "net.pipe":
                        _host.AddServiceEndpoint(type, CreateNetNamedPipeBinding(), uri);
                        break;
                    default:
                        _logger.LogInformation("No Endpoint could be provided with this Uri: {EndpointUrl}", uri);
                        break;
                }
            }
        }

        private NetTcpBinding CreateNetTcpBinding()
        {
            var binding = new NetTcpBinding(SecurityMode.None);
            if (_messageSize == 0)
            {
                binding.TransferMode = TransferMode.Streamed;
                binding.MaxReceivedMessageSize = long.MaxValue;
            }
            else
            {
                binding.MaxReceivedMessageSize = _messageSize;
            }
            binding.SendTimeout = _sendTimeout;
            binding.ReceiveTimeout = _receiveTimeout;
            return binding;
        }

        private NetNamedPipeBinding CreateNetNamedPipeBinding()
        {
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            if (_messageSize == 0)
            {
                binding.TransferMode = TransferMode.Streamed;
                binding.MaxReceivedMessageSize = long.MaxValue;
            }
            else
            {
                binding.MaxReceivedMessageSize = _messageSize;
            }
            binding.SendTimeout = _sendTimeout;
            binding.ReceiveTimeout = _receiveTimeout;
            return binding;
        }

        private BasicHttpBinding CreateBasicHttpBinding(BasicHttpSecurityMode securityMode)
        {
            var binding = new BasicHttpBinding(securityMode);
            if (_messageSize == 0)
            {
                binding.TransferMode = TransferMode.Streamed;
                binding.MaxReceivedMessageSize = long.MaxValue;
            }
            else
            {
                binding.MaxReceivedMessageSize = _messageSize;
            }
            binding.SendTimeout = _sendTimeout;
            binding.ReceiveTimeout = _receiveTimeout;
            return binding;
        }

        public void Dispose()
        {
            if (_host != null)
            {
                _host.Close();
                _host = null;
            }
        }
    }
}

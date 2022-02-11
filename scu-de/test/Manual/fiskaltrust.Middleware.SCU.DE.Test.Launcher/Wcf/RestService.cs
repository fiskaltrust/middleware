#if NET461
using fiskaltrust.Middleware.SCU.DE.Test.Launcher.Wcf.Formatting;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace fiskaltrust.Middleware.SCU.DE.Test.Launcher.Wcf
{
    public class RestService : IDisposable
    {
        private ServiceHost _host = null;
        private long _messageSize = 16 * 1024 * 1024;
        private TimeSpan _sendTimeout = TimeSpan.FromSeconds(15);
        private TimeSpan _receiveTimeout = TimeSpan.FromDays(20);
        private readonly ILogger<RestService> _logger;

        public RestService(ILogger<RestService> logger)
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

        public void ConfigureService(PackageConfiguration config, Type type, object instance, string url)
        {
            ParseConfiguration(config);
            try
            {
                ConfigureServiceBindings(type, instance, url);
                var serviceBehaviour = _host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
                if (serviceBehaviour != null)
                {
                    serviceBehaviour.InstanceContextMode = InstanceContextMode.Single;
                }
                _host.Open();
                _logger.LogInformation("{0}: {1} ({2}) - Endpoint: {3}, {4}, {5}", "REST Service", config.Package, config.Version, url, $"{url}/json", $"{url}/xml");
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Failed to start {0} {1} ({2}) - Endpoint: {3}", "REST Service", config.Package, config.Version, url);
            }
        }

        private void ConfigureServiceBindings(Type type, object instance, string url)
        {
            var baseAddress = new Uri(url.Replace("rest://", "http://"));
            _host = new WebServiceHost(instance, baseAddress);

            var debugBehavior = _host.Description.Behaviors.Find<ServiceDebugBehavior>();
            debugBehavior.IncludeExceptionDetailInFaults = true;

            var defaultEndpoint = _host.AddServiceEndpoint(type, CreateWebHttpBinding(), "");
            ConfigureJsonDateBehavior(defaultEndpoint);
            var defaultBehavior = new WebHttpBehavior
            {
                AutomaticFormatSelectionEnabled = true,
                DefaultOutgoingRequestFormat = WebMessageFormat.Json,
                DefaultOutgoingResponseFormat = WebMessageFormat.Json
            };
            defaultEndpoint.Behaviors.Add(defaultBehavior);

            var jsonEndpoint = _host.AddServiceEndpoint(type, CreateWebHttpBinding(), "json");
            ConfigureJsonDateBehavior(jsonEndpoint);
            var jsonBehavior = new WebHttpBehavior
            {
                AutomaticFormatSelectionEnabled = false,
                DefaultOutgoingRequestFormat = WebMessageFormat.Json,
                DefaultOutgoingResponseFormat = WebMessageFormat.Json,
            };
            jsonEndpoint.Behaviors.Add(jsonBehavior);

            var xmlEndpoint = _host.AddServiceEndpoint(type, CreateWebHttpBinding(), "xml");
            var xmlWebBehavior = new WebHttpBehavior
            {
                AutomaticFormatSelectionEnabled = false,
                DefaultOutgoingRequestFormat = WebMessageFormat.Xml,
                DefaultOutgoingResponseFormat = WebMessageFormat.Xml
            };
            xmlEndpoint.Behaviors.Add(xmlWebBehavior);
        }

        private void ConfigureJsonDateBehavior(ServiceEndpoint endpoint)
        {
            foreach (var operation in endpoint.Contract.Operations)
            {
                if (!operation.OperationBehaviors.OfType<ClientJsonDateFormatter>().Any())
                {
                    operation.OperationBehaviors.Add(new ClientJsonDateFormatter());
                }
            }
        }

        private WebHttpBinding CreateWebHttpBinding()
        {
            var binding = new WebHttpBinding(WebHttpSecurityMode.None);
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
#endif
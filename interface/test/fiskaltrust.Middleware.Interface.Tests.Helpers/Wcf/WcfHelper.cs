#if WCF
using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf.Formatting;

namespace fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf
{
    public static class WcfHelper
    {
        public static ServiceHost StartHost<T>(string url, T component)
        {
            var host = new ServiceHost(component, new Uri(url));
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            host.AddServiceEndpoint(typeof(T), binding, url);
            var serviceBehaviour = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            if (serviceBehaviour != null)
            {
                serviceBehaviour.InstanceContextMode = InstanceContextMode.Single;
            }
            host.Open();
            return host;
        }
        public static T GetProxy<T>(string url)
        {
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            {
                SendTimeout = TimeSpan.FromSeconds(15),
                ReceiveTimeout = TimeSpan.FromDays(14)
            };
            var factory = new ChannelFactory<T>(binding, new EndpointAddress(url));
            return factory.CreateChannel();
        }


        public static ServiceHost StartRestHost<T>(string url, T component)
        {
            var restHost = new WebServiceHost(component, new Uri(url));

            var debugBehavior = restHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            debugBehavior.IncludeExceptionDetailInFaults = true;

            var sep = restHost.AddServiceEndpoint(typeof(T), new WebHttpBinding(), "");
            var whb = sep.Behaviors.Find<WebHttpBehavior>();
            if (whb == null)
            {
                whb = new WebHttpBehavior();
                sep.Behaviors.Add(whb);
            }

            foreach (var operation in sep.Contract.Operations)
            {
                if (!operation.Behaviors.OfType<ClientJsonDateFormatter>().Any())
                {
                    operation.Behaviors.Add(new ClientJsonDateFormatter());
                }
            }

            whb.AutomaticFormatSelectionEnabled = true;
            whb.DefaultOutgoingRequestFormat = WebMessageFormat.Json;
            whb.DefaultOutgoingResponseFormat = WebMessageFormat.Json;
            var serviceBehaviour = restHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            if (serviceBehaviour != null)
            {
                serviceBehaviour.InstanceContextMode = InstanceContextMode.Single;
            }
            restHost.Open();
            return restHost;
        }

        public static T GetRestProxy<T>(string url) where T : class
        {
            var binding = new BasicHttpBinding
            {
                SendTimeout = TimeSpan.FromSeconds(15),
                ReceiveTimeout = TimeSpan.FromDays(14)
            };

            var factory = new WebChannelFactory<T>(new Uri(url));
            return factory.CreateChannel();
        }
    }
}
#endif
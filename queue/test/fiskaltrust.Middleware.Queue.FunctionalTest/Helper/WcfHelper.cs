using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace fiskaltrust.Middleware.Queue.FunctionalTest.Helper
{
    public static class WcfHelper
    {
        public static T GetClient<T>(string url) where T : class
        {
           Binding binding;

            if (url.StartsWith("http://"))
            {
                binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
                {
                    MaxReceivedMessageSize = 16 * 1024 * 1024
                };
            }
            else if (url.StartsWith("https://"))
            {
                binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
                {
                    MaxReceivedMessageSize = 16 * 1024 * 1024
                };
            }
            else if (url.StartsWith("net.pipe://"))
            {
                binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
                {
                    MaxReceivedMessageSize = 16 * 1024 * 1024
                };
            }
            else if (url.StartsWith("net.tcp://"))
            {
                binding = new NetTcpBinding(SecurityMode.None)
                {
                    MaxReceivedMessageSize = 16 * 1024 * 1024
                };
            }
            else
            {
                throw new ArgumentException($"{url} not supported", nameof(url));
            }

            var endpoint = new EndpointAddress(url);
            var factory = new ChannelFactory<T>(binding, endpoint);
            return factory.CreateChannel();
        }
    }
}
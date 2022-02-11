using System;

namespace fiskaltrust.Middleware.Queue.FunctionalTest.Helper
{
    public static class ConnectionHelper
    {
        public static T GetClient<T>(string url) where T : class
        {
            if (url.StartsWith("grpc://"))
            {
                var uri = new Uri(url);
                return GrpcHelper.GetClient<T>(uri.Host, uri.Port);
            }
            else
            {
                return WcfHelper.GetClient<T>(url);
            }
        }
    }
}
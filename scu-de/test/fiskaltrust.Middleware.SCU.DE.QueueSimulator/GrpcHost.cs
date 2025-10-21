using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.QueueSimulator;
using Grpc.Core;

namespace fiskaltrust.Middleware.Launcher
{
#pragma warning disable
    public class GrpcHost : IDisposable
    {
        private Server _host;

        public void StartService<T>(string url, T service) where T : class
        {
            if (_host != null)
            {
                _host.ShutdownAsync().RunSynchronously();
            }

            _host = GrpcHelper.StartHost(url, service);
            Console.WriteLine($"{service.GetType()} started. Listening at { url }");
        }

        public async Task ShutdownAsync() 
        { 
            await _host?.ShutdownAsync();
            _host = null;
        }

        public void Dispose()
        {
            ShutdownAsync().Wait();
        }
    }
}

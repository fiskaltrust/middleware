using fiskaltrust.storage.serialization.V0;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.AT.Test.Launcher.Grpc
{
    public class GrpcHost : IDisposable
    {
        private Server _host;
        private readonly ILogger<GrpcHost> _logger;

        public GrpcHost(ILogger<GrpcHost> logger)
        {
            _logger = logger;
        }

        public void StartService(PackageConfiguration config, string url, Type type, object service, ILoggerFactory loggerFactory)
        {
            if (_host != null)
            {
                _host.ShutdownAsync().RunSynchronously();
            }

            _host = GrpcHelper.StartHost(url, type, service, loggerFactory);
            _logger.LogInformation("{0}: {1} ({2}) - Endpoint: {3}", "gRPC Service", config.Package, config.Version, url);
        }

        public async Task ShutdownAsync()
        {
            await _host?.ShutdownAsync();
            _host = null;
        }

        public void Dispose() => ShutdownAsync().Wait();
    }
}

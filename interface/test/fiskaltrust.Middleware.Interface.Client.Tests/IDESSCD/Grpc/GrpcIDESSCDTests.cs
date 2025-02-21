using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.Middleware.Interface.Client.Tests.Helpers;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Grpc;
using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Tests.IDESSCD.Grpc
{
    public class GrpcIDESSCDTests : IDESSCDTests
    {
        private string _host = "localhost";
        private int _port = 10042;
        private static Server _server;

        ~GrpcIDESSCDTests()
        {
            Task.Run(() => _server.ShutdownAsync()).Wait();
            _server = null;
        }

        protected override ifPOS.v1.de.IDESSCD CreateClient() => GrpcDESSCDFactory.CreateSSCDAsync(new GrpcClientOptions { Url = new Uri($"http://{_host}:{_port}"), RetryPolicyOptions = _retryPolicyOptions }).Result;

        protected override void StartHost()
        {
            if (_server == null)
                _server = GrpcHelper.StartHost(_host, _port, new DummyDESSCD());
        }

        protected override void StopHost()
        {
            Task.Run(() => _server.ShutdownAsync()).Wait();
            _server = null;
        }
    }
}

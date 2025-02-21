#if GRPC

using fiskaltrust.ifPOS.Tests.Helpers;
using Grpc.Core;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Grpc;

namespace fiskaltrust.ifPOS.Tests.v1.IDESSCD
{
    public class GrpcIDESSCDTests : IDESSCDTests
    {
        private string _host = "localhost";
        private int _port = 10043;
        private static Server _server;

        protected override ifPOS.v1.de.IDESSCD CreateClient() => GrpcHelper.GetClient<ifPOS.v1.de.IDESSCD>(_host, _port);

        protected override void StartHost()
        {
            if(_server == null)
                _server = GrpcHelper.StartHost(_host, _port, new DummyDESSCD());
        }

        protected override void StopHost()
        {
            _server.KillAsync().Wait();
            _server = null;
        }
    }
}

#endif
using fiskaltrust.ifPOS.v1;
using EchoRequest = fiskaltrust.ifPOS.v2.EchoRequest;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IEchoProcessor
{
    Task<EchoResponse?> ProcessAsync(EchoRequest request);
}
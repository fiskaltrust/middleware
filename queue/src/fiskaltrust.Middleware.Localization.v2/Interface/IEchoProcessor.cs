using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IEchoProcessor
{
    Task<EchoResponse?> ProcessAsync(EchoRequest request);
}
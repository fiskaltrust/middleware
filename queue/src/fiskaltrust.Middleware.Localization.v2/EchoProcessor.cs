using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.v2;

public class EchoProcessor : IEchoProcessor
{
    public async Task<EchoResponse?> ProcessAsync(EchoRequest echoRequest)
    {
        return await Task.FromResult(new EchoResponse
        {
            Message = echoRequest.Message,
        });
    }
}

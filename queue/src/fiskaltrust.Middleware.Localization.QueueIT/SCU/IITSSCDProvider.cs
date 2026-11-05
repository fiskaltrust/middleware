using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT.SCU;

/// <summary>
/// The Italian SCU as the queue sees it: ifPOS.v2 receipts in, ifPOS.v2 receipts out. There is no
/// <c>ifPOS.v2.it</c> contract in the interface package yet; the RT devices and servers speak
/// <see cref="IITSSCD"/> (ifPOS.v1), so the provider implementation translates at the boundary. Once a v2
/// contract exists the processors only have to switch this interface.
/// </summary>
public interface IITSSCDProvider
{
    Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

    Task<RTInfo> GetRTInfoAsync();
}

public class ProcessRequest
{
    public required ReceiptRequest ReceiptRequest { get; init; }
    public required ReceiptResponse ReceiptResponse { get; init; }
}

public class ProcessResponse
{
    public required ReceiptResponse ReceiptResponse { get; init; }
}

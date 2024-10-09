using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;

public interface IGRSSCD
{
    Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

    Task<GRSSCDInfo> GetInfoAsync();
}

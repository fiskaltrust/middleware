using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;

public class GRSSCDInfo
{
}

public class InMemorySCU : IGRSSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => throw new NotImplementedException();

    public Task<GRSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}

using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v1.me;
using Org.BouncyCastle.Asn1.Crmf;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;

public class GRSSCDInfo
{
}

public class InMemorySCU : IGRSSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => throw new NotImplementedException();

    public Task<GRSSCDInfo> GetInfoAsync() => throw new NotImplementedException();
}
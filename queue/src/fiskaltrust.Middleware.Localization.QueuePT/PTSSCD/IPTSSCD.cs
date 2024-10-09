using fiskaltrust.ifPOS.v1.it;

namespace fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;

public interface IPTSSCD
{
    Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string lastHash);

    Task<PTSSCDInfo> GetInfoAsync();
}

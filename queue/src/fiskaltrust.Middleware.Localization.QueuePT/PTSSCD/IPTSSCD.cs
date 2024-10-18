using System.Runtime.Serialization;
using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;

public interface IPTSSCD
{
    Task<(ProcessResponse response, string hash)> ProcessReceiptAsync(ProcessRequest request, string lastHash);

    Task<PTSSCDInfo> GetInfoAsync();
}


public class ProcessRequest
{
    public required ReceiptRequest ReceiptRequest { get; set; }

    public required ReceiptResponse ReceiptResponse { get; set; }
}

public class ProcessResponse
{
    public required ReceiptResponse ReceiptResponse { get; set; }
}
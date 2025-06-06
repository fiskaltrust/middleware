using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;

public interface IGRSSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request, List<(ReceiptRequest, ReceiptResponse)>? receiptReferences = null);

    public Task<GRSSCDInfo> GetInfoAsync();
}

public class GRSSCDInfo
{
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
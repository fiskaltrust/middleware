using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueES.ESSSCD;

public interface IESSSCD
{
    Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

    Task<ESSSCDInfo> GetInfoAsync();
}


public class ProcessRequest
{
    public required ReceiptRequest ReceiptRequest { get; set; }

    public required ReceiptResponse ReceiptResponse { get; set; }
    public required ReceiptRequest? PreviousReceiptRequest { get; set; }
    public required ReceiptResponse? PreviousReceiptResponse { get; set; }
}

public class ProcessResponse
{
    public required ReceiptResponse ReceiptResponse { get; set; }
    public required bool Signed { get; set; }
}
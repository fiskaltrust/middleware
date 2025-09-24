using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueBE.BESSCD;

public interface IBESSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request, List<(ReceiptRequest, ReceiptResponse)> receiptReferences);

    public Task<BESSCDInfo> GetInfoAsync();
}

public class BESSCDInfo
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

// Dummy implementation for Belgium
public class DummyBESSCD : IBESSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request, List<(ReceiptRequest, ReceiptResponse)> receiptReferences)
    {
        // Dummy implementation - simply return the receipt response as is
        return Task.FromResult(new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse
        });
    }

    public Task<BESSCDInfo> GetInfoAsync()
    {
        return Task.FromResult(new BESSCDInfo());
    }
}
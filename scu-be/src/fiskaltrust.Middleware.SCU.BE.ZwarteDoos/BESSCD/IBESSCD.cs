using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueBE.BESSCD;

public interface IBESSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

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
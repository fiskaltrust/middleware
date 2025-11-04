using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.es;

namespace fiskaltrust.Middleware.Localization.QueueBE.BESSCD;

public interface IBESSCD
{
    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request);

    public Task<BESSCDInfo> GetInfoAsync();

    public Task<EchoResponse> EchoAsync(EchoRequest request);
}

public class BESSCDInfo
{
    public string? InfoData { get; set; } 
}
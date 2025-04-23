using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IProtocolCommandProcessor
{
    public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request);
}
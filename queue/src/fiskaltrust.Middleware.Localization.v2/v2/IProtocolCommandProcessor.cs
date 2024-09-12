using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2.v2
{
    public interface IProtocolCommandProcessor
    {
        Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request);
        Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request);
    }
}
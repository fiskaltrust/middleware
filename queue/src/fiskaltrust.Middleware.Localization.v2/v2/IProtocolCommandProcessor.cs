using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2;

public interface IProtocolCommandProcessor
{
    public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request);

    /// <summary>
    /// The archive receipt. A market that does not implement it returns null, and the receipt
    /// processor answers that the case is not supported, as it did before the case was routed.
    /// </summary>
    public Task<ProcessCommandResponse?> ArchiveReceipt0x3011Async(ProcessCommandRequest request) => Task.FromResult<ProcessCommandResponse?>(null);
}
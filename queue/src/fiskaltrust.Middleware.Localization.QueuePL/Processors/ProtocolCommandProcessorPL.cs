using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

public class ProtocolCommandProcessorPL : IProtocolCommandProcessor
{
    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => PLFallBackOperations.NotSupported(request, "InternalUsageMaterialConsumption");

    public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    /// <summary>
    /// Copies are served queue-side by replaying the stored response of the referenced receipt
    /// (avoids a dependency on the register's e-journal). The replay wiring lands together with
    /// the reference-resolution work; until then the case is acknowledged as a no-op.
    /// </summary>
    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);
}

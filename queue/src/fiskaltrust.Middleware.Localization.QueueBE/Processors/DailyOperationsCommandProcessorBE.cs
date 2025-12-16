using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public class DailyOperationsCommandProcessorBE : IDailyOperationsCommandProcessor
{
    // Should those not be notimplemented exceptions? (same in the invoice processor)
    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request) => await BEFallBackOperations.NoOp(request);
}
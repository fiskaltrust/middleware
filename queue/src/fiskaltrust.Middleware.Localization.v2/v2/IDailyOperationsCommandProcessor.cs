namespace fiskaltrust.Middleware.Localization.v2;

public interface IDailyOperationsCommandProcessor
{
    public Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request);
}
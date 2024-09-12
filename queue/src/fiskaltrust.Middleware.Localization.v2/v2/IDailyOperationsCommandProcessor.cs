using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2.v2
{
    public interface IDailyOperationsCommandProcessor
    {
        Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request);
        Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request);
        Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request);
    }
}
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2;

public interface ILifecycleCommandProcessor
{
    Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request);
    Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request);
    Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request);
}
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2;

public interface ILifecycleCommandProcessor
{
    public Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request);
}
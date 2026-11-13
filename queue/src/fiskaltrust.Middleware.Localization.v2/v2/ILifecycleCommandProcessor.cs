using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.v2;

public interface ILifecycleCommandProcessor
{
    public Task<ProcessCommandResponse> InitialOperationReceipt0x4001Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> OutOfOperationReceipt0x4002Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> InitSCUSwitch0x4011Async(ProcessCommandRequest request);
    public Task<ProcessCommandResponse> FinishSCUSwitch0x4012Async(ProcessCommandRequest request);

    /// <summary>
    /// Initiate migration. A market that does not implement it returns null, and the receipt
    /// processor answers that the case is not supported, as it did before the case was routed.
    /// </summary>
    public Task<ProcessCommandResponse?> InitiateMigration0x4021Async(ProcessCommandRequest request) => Task.FromResult<ProcessCommandResponse?>(null);

    /// <summary>Finish migration; null where the market does not implement it, as <see cref="InitiateMigration0x4021Async"/>.</summary>
    public Task<ProcessCommandResponse?> FinishMigration0x4022Async(ProcessCommandRequest request) => Task.FromResult<ProcessCommandResponse?>(null);
}
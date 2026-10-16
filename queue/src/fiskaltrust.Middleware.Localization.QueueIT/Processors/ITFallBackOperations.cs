using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Processors;

public static class ITFallBackOperations
{
    public static async Task<ProcessCommandResponse> NoOp(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public static Task<ProcessCommandResponse> NotSupported(ProcessCommandRequest request, string name) => throw new NotSupportedException($"The ftReceiptCase {name} - 0x{(ulong) request.ReceiptRequest.ftReceiptCase.Case():x} is not supported in the QueueIT implementation.");
}

using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public static class BEFallBackOperations
{
    public static async Task<ProcessCommandResponse> NoOp(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public static Task<ProcessCommandResponse> NotSupported(ProcessCommandRequest request, string name) => throw new NotSupportedException($"The ftReceiptCase {name} - 0x{request.ReceiptRequest.ftReceiptCase.Case():x} is not supported in the QueueBE implementation.");
}
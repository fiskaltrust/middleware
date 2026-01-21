using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public static class FallBackOperations
{
    public static async Task<ProcessCommandResponse> NoOp(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public static Task<ProcessCommandResponse> NotSupported(ProcessCommandRequest request) => throw new NotSupportedException($"The ftReceiptCase {request.ReceiptRequest.ftReceiptCase.Case()} - 0x{request.ReceiptRequest.ftReceiptCase.Case():x} is not supported in the QueueBE implementation.");

    public static Task<ProcessCommandResponse> NotYetImplemented(ProcessCommandRequest request) => throw new NotImplementedException($"The ftReceiptCase {request.ReceiptRequest.ftReceiptCase.Case()} - 0x{request.ReceiptRequest.ftReceiptCase.Case():x} is not yet implemented in the current implementation.");
}
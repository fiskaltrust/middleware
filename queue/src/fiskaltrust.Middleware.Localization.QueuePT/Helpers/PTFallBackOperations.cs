using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Constants;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public static class PTFallBackOperations
{
    public static async Task<ProcessCommandResponse> NoOp(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public static Task<ProcessCommandResponse> NotSupported(ProcessCommandRequest request, string name) => throw new NotSupportedException(ErrorMessagesPT.NotSupportedReceiptCase(request.ReceiptRequest.ftReceiptCase.Case(), name));
}

using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

public static class FRFallBackOperations
{
    /// <summary>
    /// Stores the receipt without signing it. The response still reports what was sold, so the
    /// unsigned paths carry the same <c>ftTypeOfService</c> as the signed ones.
    /// </summary>
    public static Task<ProcessCommandResponse> NoOp(ProcessCommandRequest request)
    {
        FRTypeOfServiceCalculator.Apply(request.ReceiptRequest, request.ReceiptResponse);
        return Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
    }
}

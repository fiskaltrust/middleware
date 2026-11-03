using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Reads an SCU failure off the SCU's response. An SCU reports a failure only there - ftState
/// 0x…EEEE_EEEE and the reason in a FAILURE item - because an exception does not travel the same
/// way from an SCU in the queue's process as from one behind REST. An exception from the SCU call
/// can still happen (the SCU unreachable, the transport failing); it gets the same error, since in
/// both cases the receipt is unsigned.
/// </summary>
public static class FRSSCDErrorHandling
{
    public static bool IsError(ReceiptResponse scuResponse) => scuResponse.ftState.IsState(State.Error);

    public static string Reason(ReceiptResponse scuResponse)
        => scuResponse.ftSignatures?.FirstOrDefault(x => x.Caption == "FAILURE")?.Data
            ?? $"the SCU returned ftState 0x{(ulong) scuResponse.ftState:X}";

    public static void SetSigningUnavailableError(this ReceiptResponse receiptResponse, string reason)
    {
        receiptResponse.SetReceiptResponseError($"The receipt could not be signed. NF525 requires an unbroken signature chain, so the receipt must not be issued unsigned - fix the signature creation unit and retry. Details: {reason}");
        receiptResponse.ftState = (State) StateFR.SigningUnavailableError;
    }
}

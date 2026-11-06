using System.Net.Sockets;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

/// <summary>
/// The fallback for when the SCU itself cannot be talked to. A Polish SCU never throws — device
/// failures come back as error responses it built itself — so what is left to handle here is the
/// transport between queue and SCU (a remote SCU that is down or does not answer).
/// </summary>
public static class PLSSCDErrorHandling
{
    public static bool IsScuCommunicationFailure(Exception exception)
        => exception is HttpRequestException or TaskCanceledException or SocketException
            || (exception.InnerException is not null && IsScuCommunicationFailure(exception.InnerException));

    /// <summary>
    /// A refused connection means the request never reached the SCU, so it is safe to retry. Any
    /// other transport failure (a timeout, a connection dropped mid-call) may have hit after the SCU
    /// handed the receipt to the register — then a resend could print it twice.
    /// </summary>
    public static void SetScuCommunicationError(this ReceiptResponse receiptResponse, Exception exception)
    {
        if (IsConnectionRefused(exception))
        {
            receiptResponse.SetReceiptResponseError($"The Polish SCU could not be reached, so nothing was sent to the fiscal register. Retry once the SCU is available. Details: {exception.Message}");
            return;
        }
        receiptResponse.SetReceiptResponseError($"The connection to the Polish SCU failed while it was processing the request — the register may or may not have printed the document. Do NOT resend blindly: check the register's last document first, and resend only if it was not issued. Details: {exception.Message}");
    }

    private static bool IsConnectionRefused(Exception exception)
        => exception is SocketException { SocketErrorCode: SocketError.ConnectionRefused }
            || (exception.InnerException is not null && IsConnectionRefused(exception.InnerException));
}

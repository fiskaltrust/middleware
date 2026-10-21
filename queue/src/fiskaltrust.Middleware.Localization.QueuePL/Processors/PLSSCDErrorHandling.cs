using System.Net.Sockets;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePL.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

/// <summary>
/// Maps SCU communication failures to the PL device-unreachable ftState. Detection is structural
/// (network-level exception types) plus name-based for the SCU packages' own
/// PLDeviceUnreachableException — QueuePL deliberately does not reference the SCU assemblies.
/// </summary>
public static class PLSSCDErrorHandling
{
    public static bool IsDeviceUnreachable(Exception exception)
        => exception is HttpRequestException or TaskCanceledException or SocketException
            || IsOfType(exception, "PLDeviceUnreachableException")
            || (exception.InnerException is not null && IsDeviceUnreachable(exception.InnerException));

    /// <summary>
    /// The command reached the register but its outcome is unknown (the SCU packages'
    /// PLDeviceOutcomeUnknownException, e.g. the PosNet SCU's ambiguous-response exception): the
    /// register may have printed the document, so it must be checked before anything is resent.
    /// </summary>
    public static bool IsDeviceOutcomeUnknown(Exception exception)
        => IsOfType(exception, "PLDeviceOutcomeUnknownException")
            || (exception.InnerException is not null && IsDeviceOutcomeUnknown(exception.InnerException));

    // Walks the type hierarchy so SCU-specific subclasses (e.g. the PosNet SCU's
    // ambiguous-response exception) are recognized too.
    private static bool IsOfType(Exception exception, string typeName)
    {
        for (var type = exception.GetType(); type is not null; type = type.BaseType)
        {
            if (type.Name == typeName)
            {
                return true;
            }
        }
        return false;
    }

    public static void SetDeviceUnreachableError(this ReceiptResponse receiptResponse, Exception exception)
    {
        if (IsDeviceOutcomeUnknown(exception))
        {
            receiptResponse.SetReceiptResponseError($"The Polish fiscal register did not confirm the command — it may or may not have executed it, so a receipt or report may already have been printed. Do NOT resend blindly: check the register's last document first, and resend only if it was not issued — a resend would otherwise duplicate it. Details: {exception.Message}");
        }
        else
        {
            receiptResponse.SetReceiptResponseError($"The Polish fiscal register could not be reached — without a working register no sale may legally be recorded (Art. 111(3) VAT Act). Retry once the register (or its reserve) is available. Details: {exception.Message}");
        }
        // An unknown outcome shares the unreachable state: either way there is no confirmed
        // fiscalization. Only the message differs, because the remedy does.
        receiptResponse.ftState = (State)StatePL.DeviceUnreachableError;
    }
}

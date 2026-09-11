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
            || IsPLDeviceUnreachableException(exception)
            || (exception.InnerException is not null && IsDeviceUnreachable(exception.InnerException));

    // Walks the type hierarchy so SCU-specific subclasses (e.g. the PosNet SCU's
    // ambiguous-response exception) are recognized too.
    private static bool IsPLDeviceUnreachableException(Exception exception)
    {
        for (var type = exception.GetType(); type is not null; type = type.BaseType)
        {
            if (type.Name == "PLDeviceUnreachableException")
            {
                return true;
            }
        }
        return false;
    }

    public static void SetDeviceUnreachableError(this ReceiptResponse receiptResponse, Exception exception)
    {
        receiptResponse.SetReceiptResponseError($"The Polish fiscal register could not be reached — without a working register no sale may legally be recorded (Art. 111(3) VAT Act). Retry once the register (or its reserve) is available. Details: {exception.Message}");
        receiptResponse.ftState = (State)StatePL.DeviceUnreachableError;
    }
}

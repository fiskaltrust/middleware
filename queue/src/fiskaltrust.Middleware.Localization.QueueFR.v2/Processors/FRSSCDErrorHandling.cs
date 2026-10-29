using System.Net.Sockets;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Maps SCU failures to the FR signing-unavailable ftState. Detection is structural (network-level
/// exception types, for SCUs that sign against a remote service) plus name-based for the SCU
/// packages' own exceptions — the queue deliberately does not reference an SCU implementation.
/// </summary>
public static class FRSSCDErrorHandling
{
    private static readonly string[] SigningFailureExceptionNames =
    [
        "FRSigningUnavailableException",
        "FRCertificateException",
    ];

    public static bool IsSigningUnavailable(Exception exception)
        => exception is HttpRequestException or TaskCanceledException or SocketException
            || IsFRSigningException(exception)
            || (exception.InnerException is not null && IsSigningUnavailable(exception.InnerException));

    // Walks the type hierarchy so SCU-specific subclasses are recognized too.
    private static bool IsFRSigningException(Exception exception)
    {
        for (var type = exception.GetType(); type is not null; type = type.BaseType)
        {
            if (SigningFailureExceptionNames.Contains(type.Name))
            {
                return true;
            }
        }

        return false;
    }

    public static void SetSigningUnavailableError(this ReceiptResponse receiptResponse, Exception exception)
    {
        receiptResponse.SetReceiptResponseError($"The receipt could not be signed. NF525 requires an unbroken signature chain, so the receipt must not be issued unsigned - fix the signature creation unit and retry. Details: {exception.Message}");
        receiptResponse.ftState = (State) StateFR.SigningUnavailableError;
    }
}

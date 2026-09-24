using System.Globalization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;

/// <summary>
/// The fiscal document number the SCU reports for a receipt — the register's receipt number, which
/// <see cref="FootprintComparer"/> holds against the register's last receipt number. Read by
/// signature type rather than by caption, so a reworded caption does not silently turn the
/// comparison off.
/// </summary>
public static class FiscalDocumentNumber
{
    /// <summary>The number in the response, or null when the SCU reported none.</summary>
    public static long? Of(ReceiptResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var signature = response.ftSignatures.SingleOrDefault(s => (ulong)s.ftSignatureType == (ulong)SignatureTypePL.FiscalDocumentNumber);
        return signature is null ? null : long.Parse(signature.Data, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}

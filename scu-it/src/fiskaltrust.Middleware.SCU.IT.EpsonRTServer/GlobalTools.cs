using System;
using System.Security.Cryptography;
using System.Text;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    /// <summary>
    /// Cryptographic helpers for the Epson RT Server.
    ///
    /// The Epson "EPSON Fiscal Security Protocol" (see "RT Server Security Communication Protocol" ch. 6)
    /// protects every business document with a CCDC (Codice Controllo Documento Commerciale):
    ///
    ///     CCDC = SHA-256( Section A + Section B )
    ///       Section A = the Token returned by the RT Server (first document after a Token request)
    ///                   or the CCDC of the previous document (blockchain).
    ///       Section B = the metadata representation of the business document (the printerFiscalReceipt node).
    ///
    /// Unlike the Custom RT Server (which signs with an HMAC key issued by the backend), the CCDC is a keyless
    /// SHA-256 fingerprint. This means the fingerprint can — and must — be computed locally by the middleware,
    /// exactly like the till does. There is no secret signing key kept locally; the chain is seeded by the
    /// server-issued Token instead.
    ///
    /// IMPORTANT: the exact byte layout of "Section A + Section B" (delimiters, whitespace, encoding of the
    /// metadata string) is defined by Epson and must be validated against a real device / the
    /// "Instant Lottery RT Server Implementation" and firmware behaviour. The implementation below follows the
    /// documented "SHA-256 over the concatenation of the previous fingerprint/token and the metadata" and is
    /// intentionally isolated so it can be corrected in a single place once verified on the test server.
    /// </summary>
    public static class GlobalTools
    {
        /// <summary>
        /// Computes the CCDC for a business document. Confirmed against a request accepted by firmware 6.01:
        /// CCDC = lowercase hex SHA-256 of the ENTIRE &lt;receipt&gt; element as transmitted (including the
        /// &lt;hash fingerPrint="{sectionA}"/&gt; tag, excluding &lt;receiptSecurity&gt;). The string hashed here
        /// MUST be byte-identical to the &lt;receipt&gt; element that is sent to the server.
        /// </summary>
        /// <param name="receiptElementXml">The exact &lt;receipt&gt;...&lt;/receipt&gt; string that will be transmitted.</param>
        /// <returns>Lowercase 64-character hex SHA-256 fingerprint, used in the receiptSecurity/hash tag.</returns>
        public static string ComputeCcdc(string receiptElementXml) => GetSHA256Hex(receiptElementXml);

        public static string GetSHA256Hex(string input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                // Lowercase hex, confirmed against a request accepted by the device.
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string GetSHA256Base64(string input)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }
}

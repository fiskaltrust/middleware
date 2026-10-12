using System;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The chaining rule every French SCU follows (ADR 0001, market-fr#122): the signed content of an
/// entry holds the <b>chain counter</b>, the <b>start-of-chain flag</b> and the <b>previous
/// signature</b> of the same chain. How those three are laid out in the signed content is each SCU's
/// own business; what "the previous signature" is and how the flag is coded is shared, so a chain
/// stays continuous when the SCU behind a queue changes.
/// </summary>
public static class FRChaining
{
    /// <summary>
    /// Flag value of an entry that carries over the previous signature. Coded the way Infocert's
    /// <c>TAG-*-OEN</c> field ("indication du report de signature précédente") allows it, as
    /// alphanumeric <c>O</c>/<c>N</c> (R13 v4.2, footnote 3).
    /// </summary>
    public const string PreviousSignatureCarriedOver = "O";

    /// <summary>Flag value of the first entry of a chain, which has no previous signature to carry over.</summary>
    public const string StartOfChain = "N";

    /// <summary>The start-of-chain flag for an entry signed against <paramref name="previousSignature"/>.</summary>
    public static string Flag(string? previousSignature)
        => string.IsNullOrEmpty(previousSignature) ? StartOfChain : PreviousSignatureCarriedOver;

    /// <summary>
    /// The chained signature contained in the data of an entry's <c>ReceiptSignature</c> item. A
    /// compact JWS (<c>header.payload.signature</c>, the Infocert SCU) chains on its signature
    /// segment; a bare Base64Url signature (LNE, InMemory) contains no <c>.</c> and is the chained
    /// signature as it stands. Either way the result is a substring of the stored field, never a
    /// re-encoding of it.
    /// </summary>
    public static string ChainedSignatureOf(string signatureItemData)
        => signatureItemData.Substring(signatureItemData.LastIndexOf('.') + 1);

    /// <summary>Base64Url without padding (RFC 4648 §5), the encoding every chained signature is stored in.</summary>
    public static string ToBase64Url(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

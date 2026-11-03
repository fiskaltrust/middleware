using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// <summary>The start-of-chain flag for an entry signed against <paramref name="previousSignature"/>.</summary>
    public static FRPreviousSignatureCarriedOver Flag(string? previousSignature)
        => string.IsNullOrEmpty(previousSignature) ? FRPreviousSignatureCarriedOver.No : FRPreviousSignatureCarriedOver.Yes;

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

/// <summary>
/// The start-of-chain flag: whether an entry carries over the previous signature of its chain.
/// <see cref="No"/> marks the first entry of a chain. Signed as Infocert's <c>TAG-*-OEN</c> field
/// ("indication du report de signature précédente") codes it, alphanumeric <c>O</c> (oui) /
/// <c>N</c> (non) (R13 v4.2, footnote 3): see <see cref="FRPreviousSignatureCarriedOverExt.ToCode"/>,
/// and json (de)serializes it the same way.
/// </summary>
[JsonConverter(typeof(FRPreviousSignatureCarriedOverJsonConverter))]
public enum FRPreviousSignatureCarriedOver
{
    No,
    Yes,
}

public static class FRPreviousSignatureCarriedOverExt
{
    /// <summary>The signed code: <c>O</c> for <see cref="FRPreviousSignatureCarriedOver.Yes"/>, <c>N</c> for <see cref="FRPreviousSignatureCarriedOver.No"/>.</summary>
    public static string ToCode(this FRPreviousSignatureCarriedOver self) => self == FRPreviousSignatureCarriedOver.Yes ? "O" : "N";

    public static FRPreviousSignatureCarriedOver FromCode(string code) => code switch
    {
        "O" => FRPreviousSignatureCarriedOver.Yes,
        "N" => FRPreviousSignatureCarriedOver.No,
        _ => throw new FormatException($"'{code}' is not a start-of-chain flag; expected O or N."),
    };
}

public class FRPreviousSignatureCarriedOverJsonConverter : JsonConverter<FRPreviousSignatureCarriedOver>
{
    public override FRPreviousSignatureCarriedOver Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => FRPreviousSignatureCarriedOverExt.FromCode(reader.GetString() ?? "");

    public override void Write(Utf8JsonWriter writer, FRPreviousSignatureCarriedOver value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToCode());
}

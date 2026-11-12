using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>The seller's identity as an FR SCU is configured with it (market-fr#221, ADR 0021).</summary>
public sealed record FRIssuer(
    string? Siret,
    string? Name,
    string? NameType,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? VatNumber,
    string? Rcs,
    string? Naf,
    string? LegalForm,
    string? ShareCapital,
    string? VatOption)
{
    /// <summary>The SIREN: the SIRET's first nine digits; null when the SIRET has fewer.</summary>
    public string? Siren => Siret is { Length: >= 9 } siret ? siret.Substring(0, 9) : null;
}

/// <summary>
/// The issuer item (<see cref="SignatureTypeFR.Issuer"/>): the seller's identity the receipt header must show (R13 v4.2
/// <c>ENC-TIK-SOC-*</c> on notes, tickets and payment proofs, R12 v4.2 <c>SOC-*</c> on invoices), from the SCU
/// configuration. It is printed after the POS header and is <b>not signed</b>: the R13/R12 strings carry no issuer field
/// (market-fr#221, ADR 0021). The archive copies it into the issuer columns of chains T, B, P and I.
/// </summary>
/// <remarks>
/// One line per configured value, <c>Label : value</c>, with the fixed French labels of <see cref="Labels"/> in that
/// order; a value that is not configured has no line. A reader takes the line apart at the first <c> : </c>, so a value
/// may contain anything but a line break, which is replaced by a space.
/// </remarks>
public static class FRIssuerItem
{
    public const string Caption = "Émetteur";
    public const string Separator = " : ";

    public const string SiretLabel = "SIRET";
    public const string SirenLabel = "SIREN";
    public const string NameLabel = "Dénomination";
    public const string NameTypeLabel = "Type de dénomination";
    public const string AddressLabel = "Adresse";
    public const string PostalCodeLabel = "Code postal";
    public const string CityLabel = "Ville";
    public const string CountryLabel = "Pays";
    public const string VatNumberLabel = "N° TVA intracommunautaire";
    public const string RcsLabel = "RCS";
    public const string NafLabel = "NAF";
    public const string LegalFormLabel = "Forme juridique";
    public const string ShareCapitalLabel = "Capital social";
    public const string VatOptionLabel = "Option TVA";

    /// <summary>The labels, in the order the lines are printed (ADR 0021).</summary>
    public static readonly IReadOnlyList<string> Labels =
    [
        SiretLabel, SirenLabel, NameLabel, NameTypeLabel, AddressLabel, PostalCodeLabel, CityLabel, CountryLabel,
        VatNumberLabel, RcsLabel, NafLabel, LegalFormLabel, ShareCapitalLabel, VatOptionLabel,
    ];

    /// <summary>The item's text, one <c>Label : value</c> line per configured value; null when nothing is configured.</summary>
    public static string? Data(FRIssuer issuer)
    {
        var values = new[]
        {
            issuer.Siret, issuer.Siren, issuer.Name, issuer.NameType, issuer.Address, issuer.PostalCode, issuer.City, issuer.Country,
            issuer.VatNumber, issuer.Rcs, issuer.Naf, issuer.LegalForm, issuer.ShareCapital, issuer.VatOption,
        };
        var lines = Labels.Zip(values, (label, value) => (label, value))
            .Where(x => !string.IsNullOrWhiteSpace(x.value))
            .Select(x => x.label + Separator + x.value!.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' '))
            .ToList();
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    /// <summary>The values of an item's text by label; a line without <c> : </c> is skipped.</summary>
    public static IReadOnlyDictionary<string, string> Parse(string? data)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in (data ?? "").Split('\n'))
        {
            var at = line.IndexOf(Separator, StringComparison.Ordinal);
            if (at > 0)
            {
                result[line.Substring(0, at)] = line.Substring(at + Separator.Length);
            }
        }
        return result;
    }

    /// <summary>
    /// True for the documents whose header carries the issuer: notes, tickets and payment proofs (R13) and invoices and
    /// pro formas (R12), training ones included. Closings, copies, archive receipts, JET events and technical receipts
    /// carry none.
    /// </summary>
    public static bool IsDocument(ReceiptCase receiptCase) => ((long) receiptCase & 0xFFFF) is
        0x0000 or 0x0001 or 0x0002 or 0x0003 or 0x0004 or 0x0005 or 0x0006 or 0x0007
        or 0x1000 or 0x1001 or 0x1002 or 0x1003
        or 0x3004 or 0x3005;

    /// <summary>Adds the issuer item to a document's response, printed after the header; nothing when nothing is configured.</summary>
    public static void AddIssuerItem(this ReceiptResponse response, ReceiptRequest request, FRIssuer issuer)
    {
        if (!IsDocument(request.ftReceiptCase) || Data(issuer) is not { } data)
        {
            return;
        }
        response.AddSignatureItem(SignatureTypeFR.Issuer, Caption, data, SignatureFormat.Text.WithPosition(SignatureFormatPosition.AfterHeader));
    }
}

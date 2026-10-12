using System;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The issuer claims every French SCU puts into its signed content (market-fr#141): the legal
/// identity of the seller that an R12 invoice prints (SOC-RCS, SOC-NAF, SOC-TYPE/SOC-CAPITAL,
/// SOC-REF-REGTVA, BT-34) and that the Receipt API reads back as its highest-precedence source.
/// The SIREN is not a claim: it is the first nine digits of the SIRET, which is already signed.
/// </summary>
/// <remarks>
/// The values come from the SCU configuration and are signed as configured. A claim that is not
/// configured stays empty (LNE) or absent (Infocert); the SCU never derives or guesses one.
/// </remarks>
public static class FRIssuerClaims
{
    /// <summary>VAT on debits ("option pour le paiement de la taxe d'après les débits", SOC-REF-REGTVA / BT-8).</summary>
    public const string VatOptionDebits = "debits";

    /// <summary>VAT on receipts ("encaissements", SOC-REF-REGTVA / BT-8).</summary>
    public const string VatOptionReceipts = "receipts";

    /// <summary>
    /// Rejects a configured VAT option other than <see cref="VatOptionDebits"/> or
    /// <see cref="VatOptionReceipts"/>; an empty option means "not configured".
    /// </summary>
    public static void ValidateVatOption(string scu, string? vatOption)
    {
        if (string.IsNullOrEmpty(vatOption) || vatOption == VatOptionDebits || vatOption == VatOptionReceipts)
        {
            return;
        }

        throw new FRValidationException($"The {scu} SCU accepts only '{VatOptionDebits}' or '{VatOptionReceipts}' as VatOption, not '{vatOption}'.");
    }

    /// <summary>Rejects a configured claim value that contains <paramref name="forbidden"/>, e.g. a field separator.</summary>
    public static void RejectCharacter(string scu, string name, string? value, char forbidden)
    {
        if (value is not null && value.IndexOf(forbidden) >= 0)
        {
            throw new FRValidationException($"The {scu} SCU cannot sign a {name} containing '{forbidden}'.");
        }
    }
}

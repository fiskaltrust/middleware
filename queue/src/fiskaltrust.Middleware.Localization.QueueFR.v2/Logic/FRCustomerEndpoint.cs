using System.Text.Json;
using System.Text.RegularExpressions;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The electronic address of a French business buyer that sent none (archive-v2 §1.2 <i>Customer</i>, market-fr#140,
/// BR-FR-12): <c>0225:&lt;SIREN&gt;</c>, the SIREN being the first nine digits of a 9-digit SIREN or a 14-digit SIRET
/// in <c>CustomerTaxId</c>. A business buyer is one of a B2B or B2G invoice, or one with a VAT or tax id.
/// </summary>
public static class FRCustomerEndpoint
{
    private static readonly Regex SirenOrSiret = new("^([0-9]{9}|[0-9]{14})$", RegexOptions.CultureInvariant);

    /// <summary>The inferred endpoint, or null when the buyer sent one, is not a French business, or has no valid SIREN.</summary>
    public static string? Infer(ReceiptRequest request)
    {
        if (Read(request.cbCustomer) is not { ValueKind: JsonValueKind.Object } customer || !string.IsNullOrWhiteSpace(Get(customer, "CustomerEndpointId")))
        {
            return null;
        }

        var taxId = Get(customer, "CustomerTaxId");
        var business = request.ftReceiptCase.Case() is ReceiptCase.InvoiceB2B0x1002 or ReceiptCase.InvoiceB2G0x1003
            || !string.IsNullOrWhiteSpace(Get(customer, "CustomerVATId")) || !string.IsNullOrWhiteSpace(taxId);
        if (!business || Get(customer, "CustomerCountry") != "FR" || taxId is null || !SirenOrSiret.IsMatch(taxId))
        {
            return null;
        }

        return "0225:" + taxId.Substring(0, 9);
    }

    private static JsonElement? Read(object? cbCustomer)
    {
        try
        {
            return cbCustomer switch
            {
                null => null,
                JsonElement json => json,
                string text => JsonDocument.Parse(text).RootElement.Clone(),
                var other => JsonSerializer.SerializeToElement(other),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Get(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }
        }
        return null;
    }
}

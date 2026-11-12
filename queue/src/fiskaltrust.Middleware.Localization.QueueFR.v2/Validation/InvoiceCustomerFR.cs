using System.Text.Json;
using System.Text.RegularExpressions;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using FluentValidation;
using FluentValidation.Results;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;

/// <summary>
/// The customer of an invoice signed by the InfoCert SCU (market-fr#140, decided and amended 2026-10-26): the R12
/// client block and the customer number on every invoice; for a business or public customer (B2B / B2G invoice case,
/// or a customer with a VAT or tax id) the VAT id; for a French one the SIREN in <c>CustomerTaxId</c> (9 digits, or a
/// 14-digit SIRET whose first 9 digits are the SIREN). One error per missing or invalid field, as in ES. A missing
/// <c>cbCustomer</c> is left to the shared <c>CustomerRequiredForInvoice</c>.
/// </summary>
public class InvoiceCustomerFR : AbstractValidator<ReceiptRequest>
{
    private static readonly Regex IsoAlpha2 = new("^[A-Z]{2}$", RegexOptions.CultureInvariant);
    private static readonly Regex SirenOrSiret = new("^([0-9]{9}|[0-9]{14})$", RegexOptions.CultureInvariant);

    public InvoiceCustomerFR()
    {
        RuleFor(x => x).Custom((request, context) =>
        {
            if (request.cbCustomer is null)
            {
                return;
            }

            if (Read(request.cbCustomer) is not { } customer)
            {
                context.AddFailure(new ValidationFailure("cbCustomer", "cbCustomer must be an object with the documented customer fields.") { ErrorCode = "CustomerInvalid" });
                return;
            }

            void Require(string field, string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    context.AddFailure(new ValidationFailure($"cbCustomer.{field}", $"An invoice must carry cbCustomer.{field} (R12 client).") { ErrorCode = $"{field}Missing" });
                }
            }

            Require("CustomerName", customer.Name);
            Require("CustomerId", customer.Id);
            Require("CustomerStreet", customer.Street);
            Require("CustomerZip", customer.Zip);
            Require("CustomerCity", customer.City);
            Require("CustomerCountry", customer.Country);
            if (!string.IsNullOrWhiteSpace(customer.Country) && !IsoAlpha2.IsMatch(customer.Country!))
            {
                context.AddFailure(new ValidationFailure("cbCustomer.CustomerCountry", $"cbCustomer.CustomerCountry '{customer.Country}' is not an ISO 3166-1 alpha-2 code.") { ErrorCode = "CustomerCountryInvalid" });
            }

            var business = request.ftReceiptCase.Case() is ReceiptCase.InvoiceB2B0x1002 or ReceiptCase.InvoiceB2G0x1003
                || !string.IsNullOrWhiteSpace(customer.VATId) || !string.IsNullOrWhiteSpace(customer.TaxId);
            if (!business)
            {
                return;
            }

            Require("CustomerVATId", customer.VATId);
            if (customer.Country == "FR")
            {
                Require("CustomerTaxId", customer.TaxId);
                if (!string.IsNullOrWhiteSpace(customer.TaxId) && !SirenOrSiret.IsMatch(customer.TaxId!))
                {
                    context.AddFailure(new ValidationFailure("cbCustomer.CustomerTaxId", $"cbCustomer.CustomerTaxId '{customer.TaxId}' is not a SIREN (9 digits) or a SIRET (14 digits).") { ErrorCode = "CustomerTaxIdInvalid" });
                }
            }
        });
    }

    private sealed record Customer(string? Name, string? Id, string? Street, string? Zip, string? City, string? Country, string? VATId, string? TaxId);

    private static Customer? Read(object cbCustomer)
    {
        JsonElement element;
        try
        {
            element = cbCustomer switch
            {
                JsonElement json => json,
                string text => JsonDocument.Parse(text).RootElement.Clone(),
                var other => JsonSerializer.SerializeToElement(other),
            };
        }
        catch (JsonException)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? Get(string name) => FRCaseData.String(element, name);
        return new Customer(Get("CustomerName"), Get("CustomerId"), Get("CustomerStreet"), Get("CustomerZip"), Get("CustomerCity"), Get("CustomerCountry"),
            Get("CustomerVATId"), Get("CustomerTaxId"));
    }
}

/// <summary>Reads FR case-data keys (<c>ftReceiptCaseData.FR.&lt;Key&gt;</c>, contract conventions, D2b).</summary>
internal static class FRCaseData
{
    /// <summary><c>ftReceiptCaseData.FR.ManagerId</c> (contract §1.2, D4), or null.</summary>
    public static string? ManagerId(object? caseData)
    {
        JsonElement element;
        try
        {
            element = caseData switch
            {
                null => default,
                JsonElement json => json,
                string text when !string.IsNullOrWhiteSpace(text) => JsonDocument.Parse(text).RootElement.Clone(),
                string => default,
                var other => JsonSerializer.SerializeToElement(other),
            };
        }
        catch (JsonException)
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.Object && Property(element, "FR") is { ValueKind: JsonValueKind.Object } fr ? String(fr, "ManagerId") : null;
    }

    /// <summary>A string property, matched case-insensitively; null when absent or not a string.</summary>
    public static string? String(JsonElement element, string name)
        => Property(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static JsonElement? Property(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }
        return null;
    }
}

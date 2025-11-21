using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Models;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

public static class CustomerValidations
{
    /// <summary>
    /// Validates the Portuguese customer tax ID (NIF) if provided.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateCustomerTaxId(ReceiptRequest request)
    {
        // Skip validation if no customer data is provided
        if (request.cbCustomer == null)
        {
            yield break;
        }

        MiddlewareCustomer? middlewareCustomer = null;
        try
        {
            var customerJson = JsonSerializer.Serialize(request.cbCustomer);
            middlewareCustomer = JsonSerializer.Deserialize<MiddlewareCustomer>(customerJson);
        }
        catch (JsonException)
        {
            // If deserialization fails, skip customer validation
            yield break;
        }

        if (middlewareCustomer == null)
        {
            yield break;
        }

        // If customer tax ID is provided, validate it
        if (!string.IsNullOrWhiteSpace(middlewareCustomer.CustomerVATId))
        {
            if (!PortugalValidationHelpers.IsValidPortugueseTaxId(middlewareCustomer.CustomerVATId))
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_InvalidPortugueseTaxId(middlewareCustomer.CustomerVATId),
                    "EEEE_InvalidPortugueseTaxId",
                    "cbCustomer.CustomerVATId"
                ).WithContext("ProvidedTaxId", middlewareCustomer.CustomerVATId));
            }
        }
    }
}

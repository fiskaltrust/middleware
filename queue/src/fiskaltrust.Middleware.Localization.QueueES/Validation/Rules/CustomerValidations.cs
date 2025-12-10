using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueES.Validation.Rules;

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
        bool customerDeserializationFailed = false;
        try
        {
            var customerJson = JsonSerializer.Serialize(request.cbCustomer);
            middlewareCustomer = JsonSerializer.Deserialize<MiddlewareCustomer>(customerJson);
        }
        catch (JsonException)
        {
            customerDeserializationFailed = true;
        }

        if (customerDeserializationFailed)
        {
            yield return ValidationResult.Failed(new ValidationError(
                  ErrorMessagesES.EEEE_CustomerInvalid,
                  "EEEE_CustomerInvalid",
                  "cbCustomer"
              ));
            yield break;
        }

        if (middlewareCustomer == null)
        {
            yield break;
        }


        if (string.IsNullOrEmpty(middlewareCustomer.CustomerVATId))
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_CustomerVATIdMissing,
                "EEEE_CustomerVATIdMissing",
                "cbCustomer.CustomerVATId"
            ));
        }
        else
        {
            if (!SpainValidationHelpers.IsValidSpanishTaxId(middlewareCustomer.CustomerVATId))
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesES.EEEE_InvalidPortugueseTaxId(middlewareCustomer.CustomerVATId),
                    "EEEE_InvalidPortugueseTaxId",
                    "cbCustomer.CustomerVATId"
                ).WithContext("ProvidedTaxId", middlewareCustomer.CustomerVATId));
            }
        }


        if(string.IsNullOrEmpty(middlewareCustomer.CustomerName))
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_CustomerNameMissing,
                "EEEE_CustomerNameMissing",
                "cbCustomer.CustomerName"
            ));
        }

        if (string.IsNullOrEmpty(middlewareCustomer.CustomerZip))
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_CustomerZipMissing,
                "EEEE_CustomerZipMissing",
                "cbCustomer.CustomerZip"
            ));
        }

        if (string.IsNullOrEmpty(middlewareCustomer.CustomerStreet))
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_CustomerStreetMissing,
                "EEEE_CustomerStreetMissing",
                "cbCustomer.CustomerStreet"
            ));
        }
    }
}

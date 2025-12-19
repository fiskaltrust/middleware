using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueES.Validation.Rules;

public static class CustomerValidations
{
    public static IEnumerable<ValidationResult> ValidateCustomerPresence(ReceiptRequest request)
    {
        if (request.ftReceiptCase.IsType(ReceiptCaseType.Invoice))
        {
            if (request.cbCustomer == null)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesES.EEEE_CustomerRequiredForInvoice,
                    "EEEE_CustomerMissing",
                    "cbCustomer"
                ));
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
                      ErrorMessagesES.EEEE_CustomerRequiredForInvoice,
                      "EEEE_CustomerInvalid",
                      "cbCustomer"
                  ));
                yield break;
            }
        }
    }

    /// <summary>
    /// Validates the Spanish customer tax ID (NIF) if provided.
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


        if ((middlewareCustomer.CustomerCountry == "ES" || string.IsNullOrEmpty(middlewareCustomer.CustomerCountry)) && !SpainValidationHelpers.IsValidSpanishTaxId(middlewareCustomer))
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_InvalidSpanishTaxId(middlewareCustomer.CustomerVATId),
                "EEEE_InvalidSpanishTaxId",
                "cbCustomer.CustomerVATId"
            ).WithContext("ProvidedTaxId", middlewareCustomer.CustomerVATId));
        }

        if (string.IsNullOrEmpty(middlewareCustomer.CustomerName))
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

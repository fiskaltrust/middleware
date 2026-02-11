using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Models;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

public static class cbCustomerValidations
{
    public static IEnumerable<ValidationResult> ValidateCustomerTaxId(ReceiptRequest request)
    {
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
            var rule = PortugalValidationRules.CustomerInvalid;
            yield return ValidationResult.Failed(new ValidationError(
                  ErrorMessagesPT.EEEE_CustomerInvalid,
                  rule.Code,
                  rule.Field
              ));
            yield break;
        }

        if (middlewareCustomer == null)
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(middlewareCustomer.CustomerVATId))
        {
            if (!PortugalValidationHelpers.IsValidPortugueseTaxId(middlewareCustomer.CustomerVATId))
            {
                var rule = PortugalValidationRules.InvalidPortugueseTaxId;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_InvalidPortugueseTaxId(middlewareCustomer.CustomerVATId),
                    rule.Code,
                    rule.Field
                ).WithContext("ProvidedTaxId", middlewareCustomer.CustomerVATId));
            }
        }
    }
}

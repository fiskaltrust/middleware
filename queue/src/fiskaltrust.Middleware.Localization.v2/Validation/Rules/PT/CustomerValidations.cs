using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class CustomerValidations : AbstractValidator<ReceiptRequest>
{
    public CustomerValidations()
    {
        Include(new CustomerTaxId());
    }

    public class CustomerTaxId : AbstractValidator<ReceiptRequest>
    {
        private static readonly int[] ValidFirstDigits = [1, 2, 3, 5, 6, 7, 8, 9];

        public CustomerTaxId()
        {
            RuleFor(x => x)
                .Custom((request, context) =>
                {
                    var customer = request.GetCustomerOrNull();
                    if (customer == null)
                        return;

                    if (string.IsNullOrWhiteSpace(customer.CustomerVATId))
                        return;

                    if (!IsValidPortugueseNif(customer.CustomerVATId))
                    {
                        context.AddFailure("cbCustomer.CustomerVATId",
                            $"Invalid Portuguese tax ID (NIF): '{customer.CustomerVATId}'");
                    }
                });
        }

        private static bool IsValidPortugueseNif(string taxId)
        {
            taxId = taxId.Trim();

            if (taxId.Length != 9 || !taxId.All(char.IsDigit))
                return false;

            var digits = taxId.Select(c => int.Parse(c.ToString())).ToArray();

            if (!ValidFirstDigits.Contains(digits[0]))
                return false;

            var sum = 0;
            for (var i = 0; i < 8; i++)
            {
                sum += digits[i] * (9 - i);
            }

            var remainder = sum % 11;
            var expectedCheckDigit = remainder < 2 ? 0 : 11 - remainder;

            return digits[8] == expectedCheckDigit;
        }
    }
}

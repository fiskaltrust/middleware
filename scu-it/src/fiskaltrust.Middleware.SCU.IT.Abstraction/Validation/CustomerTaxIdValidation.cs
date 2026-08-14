using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.IT.Abstraction.Validation;

/// <summary>
/// Request level validation of the customer tax identifiers. Both identifiers are optional: a receipt
/// without cbCustomer, or with an empty codice fiscale / partita IVA, is valid. A value that is present
/// but malformed fails the receipt, because the RT device would either reject it or - worse - print an
/// unusable "scontrino parlante" without telling the PoS anything.
/// </summary>
public static class CustomerTaxIdValidation
{
    public const string CustomerTaxIdErrorCaption = "it-customer-taxid-invalid";

    /// <summary>
    /// True only for receipts that actually carry a customer tax identifier worth validating.
    /// Management receipts (initial operation, out of operation, zero receipt, daily/monthly/yearly
    /// closing, reprint) are excluded on purpose: a PoS that leaves a stale cbCustomer on a Z-closing
    /// must never be prevented from closing the day.
    /// </summary>
    public static bool CarriesCustomerTaxIds(this ReceiptRequest receiptRequest)
    {
        if ((receiptRequest == null) || string.IsNullOrEmpty(receiptRequest.cbCustomer))
        {
            return false;
        }

        if (receiptRequest.IsInitialOperationReceipt() || receiptRequest.IsOutOfOperationReceipt() || receiptRequest.IsZeroReceipt())
        {
            return false;
        }

        if (receiptRequest.IsDailyClosing() || receiptRequest.IsMonthlyClosing() || receiptRequest.IsYearlyClosing())
        {
            return false;
        }

        if (receiptRequest.IsReprint())
        {
            return false;
        }

        // Malformed cbCustomer JSON yields null today and is silently ignored; that behaviour is kept.
        var customer = receiptRequest.GetCustomer();
        if (customer == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(customer.CustomerId) || !string.IsNullOrWhiteSpace(customer.CustomerVATId);
    }

    /// <summary>
    /// Validates <see cref="Customer.CustomerId"/> (codice fiscale) and <see cref="Customer.CustomerVATId"/>
    /// (partita IVA) of the request's cbCustomer. Empty fields are valid - both identifiers are optional.
    /// </summary>
    /// <param name="errorMessage">The failure reason, or null when the request is valid.</param>
    public static bool TryValidateCustomerTaxIds(this ReceiptRequest receiptRequest, out string? errorMessage)
    {
        errorMessage = null;

        var customer = receiptRequest?.GetCustomer();
        if (customer == null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(customer.CustomerId) && !ItalyValidationHelpers.IsValidCodiceFiscale(customer.CustomerId))
        {
            errorMessage = $"The given codice fiscale '{customer.CustomerId}' is not valid. cbCustomer.CustomerId must contain either a 16 character Italian codice fiscale with a valid check character, or an 11 digit partita IVA, or must be left empty.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(customer.CustomerVATId) && !ItalyValidationHelpers.IsValidPartitaIva(customer.CustomerVATId))
        {
            errorMessage = $"The given partita IVA '{customer.CustomerVATId}' is not valid. cbCustomer.CustomerVATId must contain 11 digits with a valid check digit, optionally prefixed with 'IT', or must be left empty.";
            return false;
        }

        return true;
    }
}

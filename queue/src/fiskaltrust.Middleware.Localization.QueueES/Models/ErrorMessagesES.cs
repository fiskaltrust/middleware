using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueES.Models;

public static class ErrorMessagesES
{
    public const string MultipleReceiptReferencesNotSupported = "Multiple receipt references are currently not supported.";
    public const string EEEE_CustomerInvalid = "EEEE_cbCustomer definition is invalid";

    public const string EEEE_PreviousReceiptReference = "EEEE_cbPreviousReceiptReference is mandatory and must be set for this receipt.";

    public static string EEEE_ChargeItemValidationFailed(int position, string field) => $"EEEE_Charge item at position {position}: {field} validation failed.";

    /// <summary>
    /// Error message for invalid Spanish Tax Identification Number (NIF)
    /// </summary>
    /// <param name="taxId">The invalid tax ID that was provided</param>
    /// <returns>A descriptive error message</returns>
    public static string EEEE_InvalidSpanishTaxId(string taxId) =>
        $"EEEE_Invalid Spanish Tax Identification Number (NIF): '{taxId}'. The NIF must be a 9-digit number with a valid check digit according to the Spanish tax authority validation algorithm.";


    public static string EEEE_HasBeenVoidedAlready(string receiptReference) =>
        $"EEEE_VOID for receipt '{receiptReference}' already exists. Changing the receipt state after voiding the same receipt is not allowed.";

    /// <summary>
    /// Error message for refunds missing cbPreviousReceiptReference
    /// </summary>
    public const string EEEE_RefundMissingPreviousReceiptReference = "EEEE_Refunds must have a cbPreviousReceiptReference set to identify the original receipt being refunded.";

    /// <summary>
    /// Error message for unsupported VAT rates
    /// </summary>
    public static string EEEE_UnsupportedVatRate(int position, ChargeItemCase vatRate) =>
        $"EEEE_Charge item at position {position} uses unsupported VAT rate '{vatRate}' (0x{(long) vatRate:X}).";

    /// <summary>
    /// Error message for VAT rate category not matching the specified VAT rate percentage
    /// </summary>
    public static string EEEE_VatRateMismatch(int position, ChargeItemCase vatRateCategory, decimal expectedVatRate, decimal actualVatRate) =>
        $"EEEE_Charge item at position {position}: VAT rate category '{vatRateCategory}' expects {expectedVatRate}% but VATRate property is set to {actualVatRate}%. Please ensure the VATRate matches the category.";

    /// <summary>
    /// Error message for VAT amount calculation mismatch
    /// </summary>
    public static string EEEE_VatAmountMismatch(int position, decimal providedVatAmount, decimal calculatedVatAmount, decimal difference) =>
        $"EEEE_Charge item at position {position}: VATAmount {providedVatAmount:F2} does not match the calculated VAT amount {calculatedVatAmount:F2} (difference: {difference:F2}). The difference exceeds the acceptable rounding tolerance of 0.01.";

    /// <summary>
    /// Error message for negative quantity in non-refund receipts
    /// </summary>
    public static string EEEE_NegativeQuantityNotAllowed(int position, decimal quantity) =>
        $"EEEE_Charge item at position {position}: Negative quantity ({quantity}) is not allowed in non-refund receipts. Only discounts may have negative values.";

    /// <summary>
    /// Error message for negative amount in non-refund receipts
    /// </summary>
    public static string EEEE_NegativeAmountNotAllowed(int position, decimal amount) =>
        $"EEEE_Charge item at position {position}: Negative amount ({amount:F2}) is not allowed in non-refund receipts. Only discounts may have negative values.";

    /// <summary>
    /// Error message for receipt balance mismatch between charge items and pay items
    /// </summary>
    public static string EEEE_ReceiptNotBalanced(decimal chargeItemsSum, decimal payItemsSum, decimal difference) =>
        $"EEEE_Receipt is not balanced: Sum of charge items ({chargeItemsSum:F2}) does not match sum of pay items ({payItemsSum:F2}). Difference: {difference:F2}.";

    /// <summary>
    /// Error message for unsupported charge item service type
    /// </summary>
    public static string EEEE_UnsupportedChargeItemServiceType(int position, ChargeItemCaseTypeOfService serviceType) =>
        $"EEEE_Charge item at position {position}: Type of service '{serviceType}' is not supported in Spain. Supported types: UnknownService, Delivery, OtherService, Tip, CatalogService.";

    /// <summary>
    /// Error message for full refund not matching original invoice items
    /// </summary>
    public static string EEEE_FullRefundItemsMismatch(string originalReceiptReference) =>
        $"EEEE_Full refund does not match the original invoice '{originalReceiptReference}'. All articles from the original invoice must be properly refunded with matching quantities and amounts.";

    public static string EEEE_VoidItemsMismatch(string originalReceiptReference) =>
        $"EEEE_Void does not match the original invoice '{originalReceiptReference}'. All articles from the original invoice must be properly voided with matching quantities and amounts.";

    /// <summary>
    /// Error message for missing nature of VAT (exempt reason) when VAT rate is 0
    /// </summary>
    public static string EEEE_ZeroVatRateMissingNature(int position) =>
        $"EEEE_Charge item at position {position}: When VAT rate is 0%, a valid tax exemption reason must be specified via the Nature of VAT (NN) field. ";

    public static string EEEE_UnknownTaxExemptionCode(int i, int exemptionCode) =>
        $"EEEE_Charge item at position {i}: Unknown tax exemption code '{exemptionCode}' provided. Please use a valid Spanish tax exemption code.";

    /// <summary>
    /// Error message for discount exceeding the article amount
    /// </summary>
    public static string EEEE_DiscountExceedsArticleAmount(int position, string description, decimal discountAmount, decimal articleAmount) =>
        $"EEEE_Charge item at position {position} ('{description}'): The discount amount ({discountAmount:F2}) exceeds the article amount ({articleAmount:F2}). A discount cannot be greater than the article it is applied to.";

    /// <summary>
    /// Error message for attempting to create multiple voids for the same receipt
    /// </summary>
    public static string EEEE_VoidAlreadyExists(string receiptReference) =>
        $"EEEE_A void for receipt '{receiptReference}' already exists. Multiple voids for the same receipt are not allowed.";

    public static string EEEE_OnlyEuroCurrencySupported = "EEEE_Only Euro (EUR) currency is supported for receipts in Spain.";

    public static string EEEE_InvalidCountryCodeForES = "EEEE_Invalid country code for Spain. Only 'ES' is accepted as valid country code.";

    public static string EEEE_InvalidCountryCodeInChargeItemsForES = "EEEE_Invalid country code in charge items for Spain. Only 'ES' is accepted as valid country code in charge items.";
    public static string EEEE_InvalidCountryCodeInPayItemsForES = "EEEE_Invalid country code in pay items for Spain. Only 'ES' is accepted as valid country code in pay items.";

    public static string EEEE_CustomerNameMissing = "EEEE_Customer name is mandatory and cannot be null or empty.";

    public static string EEEE_CustomerZipMissing = "EEEE_Customer zip code is mandatory and cannot be null or empty.";

    public static string EEEE_CustomerStreetMissing = "EEEE_Customer street is mandatory and cannot be null or empty.";
    public static string EEEE_ChargeItemsMissing = "EEEE_ChargeItems must not be null.";
    public static string EEEE_PayItemsMissing = "EEEE_PayItems must not be null.";
    public static string EEEE_CustomerRequiredForInvoice = "EEEE_Customer information is mandatory for Invoice receipts and cannot be null. Make sure to fill the field cbCustomer";
}

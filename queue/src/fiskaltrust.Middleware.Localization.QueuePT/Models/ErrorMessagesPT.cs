using System;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models;

public static class ErrorMessagesPT
{
    public static string UnknownReceiptCase(long caseCode) => $"The given ftReceiptCase 0x{caseCode:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
    
    public static string NotSupportedReceiptCase(ReceiptCase caseCode, string name) => $"The ftReceiptCase {name} - 0x{caseCode:x} is not supported in the QueuePT implementation.";
    
    public const string PreviousReceiptReferenceNotFound = "The given cbPreviousReceiptReference didn't match with any of the items in the Queue.";
    
    public const string MultipleReceiptReferencesNotSupported = "Multiple receipt references are currently not supported.";
    
    public const string VoidNotSupported = "Void is not supported";

    // Validation error messages with EEEE_ prefix
    public const string EEEE_BottleLessThanOneLiterNotAllowed = "EEEE_Articles classified as 'Garrafão < 1 litro' are not allowed in this receipt type.";
    
    public const string EEEE_ChargeItemDescriptionTooShort = "EEEE_Charge item description must be at least 3 characters long.";
    
    public const string EEEE_ChargeItemDescriptionMissing = "EEEE_Charge item description is mandatory and cannot be null or empty.";
    
    public const string EEEE_ChargeItemVATRateMissing = "EEEE_Charge item VAT rate is mandatory and must be set.";
    
    public const string EEEE_ChargeItemAmountMissing = "EEEE_Charge item amount (price) is mandatory and must be set.";
    
    public const string EEEE_UserMissing = "EEEE_cbUser is mandatory and must be set for this receipt.";

    public static string EEEE_ChargeItemValidationFailed(int position, string field) => $"EEEE_Charge item at position {position}: {field} validation failed.";

    /// <summary>
    /// Error message for invalid Portuguese Tax Identification Number (NIF)
    /// </summary>
    /// <param name="taxId">The invalid tax ID that was provided</param>
    /// <returns>A descriptive error message</returns>
    public static string EEEE_InvalidPortugueseTaxId(string taxId) => 
        $"EEEE_Invalid Portuguese Tax Identification Number (NIF): '{taxId}'. The NIF must be a 9-digit number with a valid check digit according to the Portuguese tax authority validation algorithm.";

    /// <summary>
    /// Error message for cash payment exceeding 3000€ limit
    /// </summary>
    public const string EEEE_CashPaymentExceedsLimit = "EEEE_Individual cash payment exceeds the legal limit of 3000€. No single cash payment can exceed this amount in Portugal.";

    /// <summary>
    /// Error message for POS receipt exceeding 1000€ net amount limit
    /// </summary>
    public const string EEEE_PosReceiptNetAmountExceedsLimit = "EEEE_Point of Sale receipt net amount exceeds the legal limit of 1000€. Receipts with net amounts above this limit require a different document type.";

    /// <summary>
    /// Error message for OtherService charge items exceeding 100€ net amount limit
    /// </summary>
    public const string EEEE_OtherServiceNetAmountExceedsLimit = "EEEE_The sum of OtherService charge items exceeds the legal limit of 100€ net amount. Services must not exceed this limit on a Point of Sale receipt.";

    public static string EEEE_CbReceiptMomentBeforeLastMoment(string seriesIdentifier, DateTime lastMoment) =>
        $"EEEE_cbReceiptMoment ({lastMoment:O}) must not be earlier than the last recorded cbReceiptMoment for series '{seriesIdentifier}'. Only handwritten receipts may be backdated.";

    /// <summary>
    /// Error message for attempting to create multiple refunds for the same receipt
    /// </summary>
    public static string EEEE_RefundAlreadyExists(string receiptReference) =>
        $"EEEE_A refund for receipt '{receiptReference}' already exists. Multiple refunds for the same receipt are not allowed.";

    /// <summary>
    /// Error message for refunds missing cbPreviousReceiptReference
    /// </summary>
    public const string EEEE_RefundMissingPreviousReceiptReference = "EEEE_Refunds must have a cbPreviousReceiptReference set to identify the original receipt being refunded.";

    /// <summary>
    /// Error message for unsupported VAT rates
    /// </summary>
    public static string EEEE_UnsupportedVatRate(int position, ChargeItemCase vatRate) =>
        $"EEEE_Charge item at position {position} uses unsupported VAT rate '{vatRate}' (0x{(long)vatRate:X}). Portugal only supports: DiscountedVatRate1 (RED/6%), DiscountedVatRate2 (INT/13%), NormalVatRate (NOR/23%), and NotTaxable (ISE).";

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
}

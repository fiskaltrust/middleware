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
    
    public const string EEEE_ChargeItemDescriptionTooShort = "EEEE_Charge item description must be longer than 3 characters.";
    
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
}

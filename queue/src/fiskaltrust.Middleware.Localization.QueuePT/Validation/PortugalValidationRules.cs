using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation;

public sealed record ValidationRuleDefinition(string Code, string Description, string? Field = null);

public static class PortugalValidationFields
{
    public static readonly string ReceiptCase = nameof(ReceiptRequest.ftReceiptCase);
    public static readonly string ReceiptCaseData = nameof(ReceiptRequest.ftReceiptCaseData);
    public static readonly string ReceiptCaseFlags = nameof(ReceiptRequest.ftReceiptCase);
    public static readonly string ChargeItems = nameof(ReceiptRequest.cbChargeItems);
    public static readonly string PayItems = nameof(ReceiptRequest.cbPayItems);
    public static readonly string ChargeItemsDescription = $"{nameof(ReceiptRequest.cbChargeItems)}.{nameof(ChargeItem.Description)}";
    public static readonly string ChargeItemsVatRate = $"{nameof(ReceiptRequest.cbChargeItems)}.{nameof(ChargeItem.VATRate)}";
    public static readonly string ChargeItemsAmount = $"{nameof(ReceiptRequest.cbChargeItems)}.{nameof(ChargeItem.Amount)}";
    public static readonly string ChargeItemsQuantity = $"{nameof(ReceiptRequest.cbChargeItems)}.{nameof(ChargeItem.Quantity)}";
    public static readonly string ChargeItemsVatAmount = $"{nameof(ReceiptRequest.cbChargeItems)}.{nameof(ChargeItem.VATAmount)}";
    public static readonly string ChargeItemsCase = $"{nameof(ReceiptRequest.cbChargeItems)}.{nameof(ChargeItem.ftChargeItemCase)}";
    public static readonly string PreviousReceiptReference = nameof(ReceiptRequest.cbPreviousReceiptReference);
    public static readonly string ReceiptMoment = nameof(ReceiptRequest.cbReceiptMoment);
    public static readonly string ReceiptMomentAndFtReceiptMoment = $"{nameof(ReceiptRequest.cbReceiptMoment)},{nameof(ReceiptResponse.ftReceiptMoment)}";
    public static readonly string Customer = nameof(ReceiptRequest.cbCustomer);
    public static readonly string CustomerVatId = $"{nameof(ReceiptRequest.cbCustomer)}.{nameof(MiddlewareCustomer.CustomerVATId)}";
    public static readonly string User = nameof(ReceiptRequest.cbUser);
    public static readonly string Currency = nameof(ReceiptRequest.Currency);
}

/// <summary>
/// Documentation of all QueuePT validation rules (code, description, default field).
/// </summary>
public static class PortugalValidationRules
{
    public static readonly ValidationRuleDefinition ReceiptNotBalanced =
        new("EEEE_ReceiptNotBalanced", "Sum of charge items must match sum of pay items (delivery notes are exempt).");

    public static readonly ValidationRuleDefinition OtherServiceNetAmountExceedsLimit =
        new("EEEE_OtherServiceNetAmountExceedsLimit", "OtherService net amount must not exceed 100 EUR.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition RefundMissingPreviousReceiptReference =
        new("EEEE_RefundMissingPreviousReceiptReference", "Refunds must reference a previous receipt.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition CbReceiptMomentInFuture =
        new("EEEE_CbReceiptMomentInFuture", "cbReceiptMoment must not be in the future.", PortugalValidationFields.ReceiptMoment);

    public static readonly ValidationRuleDefinition CbReceiptMomentDeviationExceeded =
        new("EEEE_CbReceiptMomentDeviationExceeded", "cbReceiptMoment must be within 10 minutes of server time for non-handwritten receipts.", PortugalValidationFields.ReceiptMoment);

    public static readonly ValidationRuleDefinition CbReceiptMomentNotUtc =
        new("EEEE_CbReceiptMomentNotUtc", "cbReceiptMoment must be in UTC for time difference validation.", PortugalValidationFields.ReceiptMoment);

    public static readonly ValidationRuleDefinition ReceiptMomentTimeDifferenceExceeded =
        new("EEEE_ReceiptMomentTimeDifferenceExceeded", "cbReceiptMoment and ftReceiptMoment must not differ by more than 2 minutes.", PortugalValidationFields.ReceiptMomentAndFtReceiptMoment);

    public static readonly ValidationRuleDefinition InvalidPositions =
        new("EEEE_InvalidPositions", "Item positions must start at 1 and be strictly increasing without gaps.");

    public static readonly ValidationRuleDefinition CustomerInvalid =
        new("EEEE_CustomerInvalid", "cbCustomer must be a valid MiddlewareCustomer object.", PortugalValidationFields.Customer);

    public static readonly ValidationRuleDefinition InvalidPortugueseTaxId =
        new("EEEE_InvalidPortugueseTaxId", "Customer VAT ID (NIF) must be valid.", PortugalValidationFields.CustomerVatId);

    public static readonly ValidationRuleDefinition ChargeItemDescriptionMissing =
        new("EEEE_ChargeItemDescriptionMissing", "Charge item description is mandatory.", PortugalValidationFields.ChargeItemsDescription);

    public static readonly ValidationRuleDefinition ChargeItemDescriptionEncodingInvalid =
        new("EEEE_ChargeItemDescriptionEncodingInvalid", "Charge item description must be representable in Windows-1252 encoding.", PortugalValidationFields.ChargeItemsDescription);

    public static readonly ValidationRuleDefinition ChargeItemVatRateMissing =
        new("EEEE_ChargeItemVATRateMissing", "Charge item VAT rate is mandatory.", PortugalValidationFields.ChargeItemsVatRate);

    public static readonly ValidationRuleDefinition ChargeItemAmountMissing =
        new("EEEE_ChargeItemAmountMissing", "Charge item amount is mandatory.", PortugalValidationFields.ChargeItemsAmount);

    public static readonly ValidationRuleDefinition ChargeItemQuantityZeroNotAllowed =
        new("EEEE_ChargeItemQuantityZeroNotAllowed", "Charge item quantity must not be zero.", PortugalValidationFields.ChargeItemsQuantity);

    public static readonly ValidationRuleDefinition ChargeItemDescriptionTooShort =
        new("EEEE_ChargeItemDescriptionTooShort", "Charge item description must be at least 3 characters long.", PortugalValidationFields.ChargeItemsDescription);

    public static readonly ValidationRuleDefinition PosReceiptNetAmountExceedsLimit =
        new("EEEE_PosReceiptNetAmountExceedsLimit", "Point of sale receipt net amount must not exceed 1000 EUR.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition UnsupportedVatRate =
        new("EEEE_UnsupportedVatRate", "Only supported VAT rates may be used in charge items.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition UnsupportedChargeItemServiceType =
        new("EEEE_UnsupportedChargeItemServiceType", "Only supported charge item service types may be used.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition VatRateMismatch =
        new("EEEE_VatRateMismatch", "VAT rate category must match the VATRate percentage.", PortugalValidationFields.ChargeItemsVatRate);

    public static readonly ValidationRuleDefinition VatAmountMismatch =
        new("EEEE_VatAmountMismatch", "VATAmount must match the calculated VAT amount within tolerance.", PortugalValidationFields.ChargeItemsVatAmount);

    public static readonly ValidationRuleDefinition DiscountVatRateOrCaseMismatch =
        new("EEEE_DiscountVatRateOrCaseMismatch", "Discounts/extras must use the same VAT rate and VAT case as their related line item.", PortugalValidationFields.ChargeItemsCase);

    public static readonly ValidationRuleDefinition PositiveDiscountNotAllowed =
        new("EEEE_PositiveDiscountNotAllowed", "Discounts/extras must not be positive.", PortugalValidationFields.ChargeItemsAmount);

    public static readonly ValidationRuleDefinition NegativeQuantityNotAllowed =
        new("EEEE_NegativeQuantityNotAllowed", "Negative quantities are not allowed for non-refund items.", PortugalValidationFields.ChargeItemsQuantity);

    public static readonly ValidationRuleDefinition NegativeAmountNotAllowed =
        new("EEEE_NegativeAmountNotAllowed", "Negative amounts are not allowed for non-refund items.", PortugalValidationFields.ChargeItemsAmount);

    public static readonly ValidationRuleDefinition ZeroVatRateMissingNature =
        new("EEEE_ZeroVatRateMissingNature", "VAT rate 0 requires a valid nature of VAT (exemption).", PortugalValidationFields.ChargeItemsCase);

    public static readonly ValidationRuleDefinition UnknownTaxExemptionCode =
        new("EEEE_UnknownTaxExemptionCode", "Nature of VAT exemption code must be valid.", PortugalValidationFields.ChargeItemsCase);

    public static readonly ValidationRuleDefinition DiscountExceedsArticleAmount =
        new("EEEE_DiscountExceedsArticleAmount", "Discounts must not exceed the related article amount.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition UserTooShort =
        new("EEEE_UserTooShort", "cbUser must be at least 3 characters.", PortugalValidationFields.User);

    public static readonly ValidationRuleDefinition InvalidUserStructure =
        new("EEEE_InvalidUserStructure", "cbUser must follow PTUserObject structure.", PortugalValidationFields.User);

    public static readonly ValidationRuleDefinition CashPaymentExceedsLimit =
        new("EEEE_CashPaymentExceedsLimit", "Individual cash payment must not exceed 3000 EUR.", PortugalValidationFields.PayItems);

    public static readonly ValidationRuleDefinition InvalidCountryCodeForPT =
        new("EEEE_InvalidCountryCodeForPT", "Receipt country must be PT.", PortugalValidationFields.ReceiptCase);

    public static readonly ValidationRuleDefinition ChargeItemsMissing =
        new("EEEE_ChargeItemsMissing", "cbChargeItems must not be null.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition PayItemsMissing =
        new("EEEE_PayItemsMissing", "cbPayItems must not be null.", PortugalValidationFields.PayItems);

    public static readonly ValidationRuleDefinition InvalidCountryCodeInChargeItemsForPT =
        new("EEEE_InvalidCountryCodeInChargeItemsForPT", "Charge item country must be PT.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition InvalidCountryCodeInPayItemsForPT =
        new("EEEE_InvalidCountryCodeInPayItemsForPT", "Pay item country must be PT.", PortugalValidationFields.PayItems);

    public static readonly ValidationRuleDefinition OnlyEuroCurrencySupported =
        new("EEEE_OnlyEuroCurrencySupported", "Only EUR currency is supported.", PortugalValidationFields.Currency);

    public static readonly ValidationRuleDefinition PreviousReceiptReference =
        new("EEEE_PreviousReceiptReference", "cbPreviousReceiptReference must be set.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition PreviousReceiptLineItemMismatch =
        new("EEEE_PreviousReceiptLineItemMismatch", "Previous receipt must share at least one matching line item.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition HandwrittenReceiptsNotSupported =
        new("EEEE_HandwrittenReceiptsNotSupported", "Handwritten receipts cannot be used with refund/void/partial refund.", PortugalValidationFields.ReceiptCase);

    public static readonly ValidationRuleDefinition HandwrittenReceiptSeriesAndNumberMandatory =
        new("EEEE_HandwrittenReceiptSeriesAndNumberMandatory", "Handwritten receipts require Series and Number in ftReceiptCaseData.", PortugalValidationFields.ReceiptCaseData);

    public static readonly ValidationRuleDefinition HandwrittenReceiptSeriesInvalidCharacter =
        new("EEEE_HandwrittenReceiptSeriesInvalidCharacter", "Handwritten receipt series contains invalid characters.", PortugalValidationFields.ReceiptCaseData);

    public static readonly ValidationRuleDefinition PreviousReceiptIsVoided =
        new("EEEE_PreviousReceiptIsVoided", "Referenced receipt has already been voided.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition PaymentTransferRequiresAccountReceivableItem =
        new("EEEE_PaymentTransferRequiresAccountReceivableItem", "Payment transfer requires at least one receivable charge item.", PortugalValidationFields.ChargeItems);

    public static readonly ValidationRuleDefinition RefundAlreadyExists =
        new("EEEE_RefundAlreadyExists", "A refund or payment transfer already exists for the referenced receipt.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition WorkingDocumentAlreadyInvoiced =
        new("EEEE_WorkingDocumentAlreadyInvoiced", "Working document has already been invoiced and cannot be voided.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition VoidAlreadyExists 
        = new("EEEE_VoidAlreadyExists", "A void already exists for the referenced receipt.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition CannotVoidInvoicedDocument =
        new("EEEE_CannotVoidInvoicedDocument", "A invoiced document cannot be voided.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition MixedRefundItemsNotAllowed =
        new("EEEE_MixedRefundItemsNotAllowed", "Partial refunds must not mix refund and non-refund items.");

    public static readonly ValidationRuleDefinition MixedRefundPayItemsNotAllowed =
        new("EEEE_MixedRefundPayItemsNotAllowed", "Partial refunds must not mix refund and non-refund pay items.", PortugalValidationFields.PayItems);

    public static readonly ValidationRuleDefinition PayItemsMissingForRefund =
        new("EEEE_PayItemsMissingForRefund", "Partial refunds with charge items must include pay items.", PortugalValidationFields.PayItems);

    public static readonly ValidationRuleDefinition TransportationIsNotSupported =
        new("EEEE_TransportationIsNotSupported", "TransportInformation is not supported in Portugal.", PortugalValidationFields.ReceiptCaseFlags);

    public static readonly ValidationRuleDefinition PaymentTransferForRefundedReceipt =
        new("EEEE_PaymentTransferForRefundedReceipt", "Payment transfer cannot be created for a receipt that has been refunded.", PortugalValidationFields.PreviousReceiptReference);

    public static readonly ValidationRuleDefinition PaymentTransferCustomerMismatch =
        new("EEEE_PaymentTransferCustomerMismatch", "Customer data must match between payment transfer and original invoice.", PortugalValidationFields.Customer);

    public static readonly ValidationRuleDefinition PaymentTransferExceedsRemainingAmount =
        new("EEEE_PaymentTransferExceedsRemainingAmount", "Payment transfer amount exceeds the remaining amount after partial refunds.", PortugalValidationFields.PayItems);

    public static readonly ValidationRuleDefinition HandwrittenReceiptSeriesNumberAlreadyLinked =
        new("EEEE_HandwrittenReceiptSeriesNumberAlreadyLinked", "A handwritten receipt with the same series and number has already been linked.", PortugalValidationFields.ReceiptCaseData);

    public static readonly IReadOnlyList<ValidationRuleDefinition> All = new[]
    {
        ReceiptNotBalanced,
        OtherServiceNetAmountExceedsLimit,
        RefundMissingPreviousReceiptReference,
        CbReceiptMomentInFuture,
        CbReceiptMomentDeviationExceeded,
        CbReceiptMomentNotUtc,
        ReceiptMomentTimeDifferenceExceeded,
        InvalidPositions,
        CustomerInvalid,
        InvalidPortugueseTaxId,
        ChargeItemDescriptionMissing,
        ChargeItemDescriptionEncodingInvalid,
        ChargeItemVatRateMissing,
        ChargeItemAmountMissing,
        ChargeItemQuantityZeroNotAllowed,
        ChargeItemDescriptionTooShort,
        PosReceiptNetAmountExceedsLimit,
        UnsupportedVatRate,
        UnsupportedChargeItemServiceType,
        VatRateMismatch,
        VatAmountMismatch,
        DiscountVatRateOrCaseMismatch,
        PositiveDiscountNotAllowed,
        NegativeQuantityNotAllowed,
        NegativeAmountNotAllowed,
        ZeroVatRateMissingNature,
        UnknownTaxExemptionCode,
        DiscountExceedsArticleAmount,
        UserTooShort,
        InvalidUserStructure,
        CashPaymentExceedsLimit,
        InvalidCountryCodeForPT,
        ChargeItemsMissing,
        PayItemsMissing,
        InvalidCountryCodeInChargeItemsForPT,
        InvalidCountryCodeInPayItemsForPT,
        OnlyEuroCurrencySupported,
        PreviousReceiptReference,
        PreviousReceiptLineItemMismatch,
        HandwrittenReceiptsNotSupported,
        HandwrittenReceiptSeriesAndNumberMandatory,
        PreviousReceiptIsVoided,
        PaymentTransferRequiresAccountReceivableItem,
        RefundAlreadyExists,
        WorkingDocumentAlreadyInvoiced,
        VoidAlreadyExists,
        MixedRefundItemsNotAllowed,
        MixedRefundPayItemsNotAllowed,
        PayItemsMissingForRefund,
        TransportationIsNotSupported,
        HandwrittenReceiptSeriesNumberAlreadyLinked
    };


}

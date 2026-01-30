using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public enum DocumentStatus
{
    Unknown,
    NotReferenced,
    Invoiced,
    Voided,
    Refunded,
    PartiallyRefunded
}

public sealed record DocumentStatusState(DocumentStatus Status, (ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)? SourceReceipt = null, List<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>? RelatedReceipts = null);

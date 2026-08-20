using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;

public enum ReceiptCaseFlags : long
{
    HasTransportInformation = 0x0000_0000_0400_0000,

    /// <summary>
    /// local flag for:
    /// Απλοποιημένο Τιμολόγιο — simplified invoice, myDATA document type 11.3 (market-gr#265).
    /// An INVOICE to a business in legally simplified form (ΕΛΠ ν.4308/2014 άρθρο 10), not a retail
    /// receipt: the buyer's details are omitted, which is why 11.3 is a
    /// «Μη Αντικριζόμενο» / non-counterparty document.
    /// </summary>
    IsSimplifiedInvoice = 0x0000_0001_0000_0000
}

public static class ReceiptCaseFlagsExt
{
    public static ReceiptCase WithFlag(this ReceiptCase self, ReceiptCaseFlags flag) => (ReceiptCase) ((long) self | (long) flag);
    public static bool IsFlag(this ReceiptCase self, ReceiptCaseFlags flag) => ((long) self & (long) flag) == (long) flag;
}
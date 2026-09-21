using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;

/// <summary>
/// How every Polish SCU reads a receipt case, so the InMemory SCU and a device-backed one cannot
/// disagree about which documents are fiscal or about what a case they all refuse is told to the POS.
/// </summary>
public static class PLReceiptCases
{
    /// <summary>
    /// Only the fiscal sale documents consume a fiscal document number on the register —
    /// non-fiscal receipt cases (payment transfer, sale without fiscalization obligation,
    /// delivery note, table check, pro forma) do not.
    /// </summary>
    public static bool IsFiscalReceipt(this ReceiptCase receiptCase)
        => receiptCase.IsType(ReceiptCaseType.Receipt)
            && (receiptCase.IsCase(ReceiptCase.UnknownReceipt0x0000)
                || receiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001)
                || receiptCase.IsCase(ReceiptCase.ECommerce0x0004));

    /// <summary>What a POS is told when it sends an invoice case to a Polish SCU.</summary>
    public const string InvoiceCaseRefusal =
        "Invoice cases (0x1xxx) must not reach a Polish SCU — QueuePL persists them without fiscalization.";

    /// <summary>The caption the daily report number is signed under, in the wording the register prints.</summary>
    public const string ZReportNumberCaption = "Numer raportu dobowego";
}

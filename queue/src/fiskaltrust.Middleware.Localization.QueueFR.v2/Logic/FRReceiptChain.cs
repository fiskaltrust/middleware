using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// France signs and chains several independent receipt chains in parallel. Each chain has its own
/// national up-counting numerator and carries the signature of its own previous entry (ADR 0001).
/// The ftReceiptCase of a receipt decides which chain it belongs to, so the queue resolves the
/// chain once and hands the SCU that chain's previous signature.
/// </summary>
/// <remarks>
/// Each closing type has its own closing chain, and the receipts that are neither sales documents
/// nor closings go to the technical chain (ADR 0002, market-fr#114). The dictionaries chain each
/// grand total to the previous grand total of the same periodicity (R12 v4.2 §7 fn 5, R13 v4.2 §7
/// fn 6). The prefixes are persisted in ftReceiptIdentification, so they must not change once a
/// v2 queue is live.
/// </remarks>
public enum FRReceiptChain
{
    /// <summary>Sales receipts (tickets).</summary>
    Ticket,

    /// <summary>Payment proofs (justificatifs de paiement).</summary>
    PaymentProof,

    /// <summary>Invoices (factures).</summary>
    Invoice,

    /// <summary>
    /// The technical chain: zero receipts, the other daily operations that are not closings, and
    /// the lifecycle receipts.
    /// </summary>
    Technical,

    /// <summary>Non-fiscal documents handed to the customer: bills, pro formas, delivery notes.</summary>
    Bill,

    /// <summary>Technical event and accounting logs.</summary>
    Log,

    /// <summary>Duplicates of an already issued document.</summary>
    Duplicate,

    /// <summary>Training mode, kept strictly apart from the fiscal chains.</summary>
    Training,

    /// <summary>The closing chain of shift closings.</summary>
    ShiftClosing,

    /// <summary>The closing chain of daily closings.</summary>
    DailyClosing,

    /// <summary>The closing chain of monthly closings.</summary>
    MonthlyClosing,

    /// <summary>The closing chain of yearly closings.</summary>
    YearlyClosing,

    /// <summary>
    /// Archive receipts. As in v1.2, each one closes an archive period and chains on the previous
    /// archive receipt's signature (market-fr#221, decided 2026-11-09).
    /// </summary>
    Archive,
}

public static class FRReceiptChainExt
{
    /// <summary>The archive receipt; ifPOS has no name for the case yet.</summary>
    public const ReceiptCase ArchiveReceipt = (ReceiptCase) 0x3011;

    /// <summary>The letter that prefixes the national numbering of this chain.</summary>
    public static string Identifier(this FRReceiptChain chain) => chain switch
    {
        FRReceiptChain.Ticket => "T",
        FRReceiptChain.PaymentProof => "P",
        FRReceiptChain.Invoice => "I",
        FRReceiptChain.Technical => "E",
        FRReceiptChain.Bill => "B",
        FRReceiptChain.Log => "L",
        FRReceiptChain.Duplicate => "D",
        FRReceiptChain.Training => "X",
        FRReceiptChain.ShiftClosing => "S",
        FRReceiptChain.DailyClosing => "Z",
        FRReceiptChain.MonthlyClosing => "M",
        FRReceiptChain.YearlyClosing => "Y",
        FRReceiptChain.Archive => "A",
        _ => "T",
    };

    /// <summary>
    /// Resolves the chain a receipt is signed into. Training receipts never enter a fiscal chain,
    /// so the training flag takes precedence over the case.
    /// </summary>
    public static FRReceiptChain ResolveChain(this ReceiptRequest request)
    {
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return FRReceiptChain.Training;
        }

        var receiptCase = request.ftReceiptCase.Case();

        if (request.ftReceiptCase.IsType(ReceiptCaseType.Invoice))
        {
            return FRReceiptChain.Invoice;
        }

        if (request.ftReceiptCase.IsType(ReceiptCaseType.DailyOperations))
        {
            return receiptCase switch
            {
                ReceiptCase.ShiftClosing0x2010 => FRReceiptChain.ShiftClosing,
                ReceiptCase.DailyClosing0x2011 => FRReceiptChain.DailyClosing,
                ReceiptCase.MonthlyClosing0x2012 => FRReceiptChain.MonthlyClosing,
                ReceiptCase.YearlyClosing0x2013 => FRReceiptChain.YearlyClosing,
                _ => FRReceiptChain.Technical,
            };
        }

        if (request.ftReceiptCase.IsType(ReceiptCaseType.Lifecycle))
        {
            return FRReceiptChain.Technical;
        }

        if (request.ftReceiptCase.IsType(ReceiptCaseType.Log))
        {
            // As v1.2 through the PosSystem API v2→v0 translation: a pay protocol is a payment
            // proof (v0 0x02), an order a bill (v0 0x08).
            return receiptCase switch
            {
                ReceiptCase.CopyReceiptPrintExistingReceipt0x3010 => FRReceiptChain.Duplicate,
                ReceiptCase.Pay0x3005 => FRReceiptChain.PaymentProof,
                ReceiptCase.Order0x3004 => FRReceiptChain.Bill,
                ArchiveReceipt => FRReceiptChain.Archive,
                _ => FRReceiptChain.Log,
            };
        }

        // A sale without fiscalization obligation reaches v1.2 as a foreign sale (v0 0x0E), which
        // is chained into the bills and kept out of the grand totals. An e-commerce sale and a sale
        // flagged as made in a foreign country are tickets: NF525 exempts neither from the grand
        // totals, and the PosSystem API drops the flag, so v1.2 in production counted them too
        // (market-fr#221). Whether a sale bears French VAT is told by its charge items, not here.
        return receiptCase switch
        {
            ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 => FRReceiptChain.Bill,
            ReceiptCase.PaymentTransfer0x0002 => FRReceiptChain.PaymentProof,
            ReceiptCase.DeliveryNote0x0005 => FRReceiptChain.Bill,
            (ReceiptCase) 0x0006 /* Table Check */ => FRReceiptChain.Bill,
            (ReceiptCase) 0x0007 /* Pro Forma */ => FRReceiptChain.Bill,
            _ => FRReceiptChain.Ticket,
        };
    }
}

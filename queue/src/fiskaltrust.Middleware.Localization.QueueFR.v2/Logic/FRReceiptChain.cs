using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// France signs and chains several independent receipt chains in parallel, each with its own
/// national up-counting numerator and its own last hash — the layout <c>ftQueueFR</c> documents.
/// Which chain a receipt belongs to follows from its ftReceiptCase, so the queue resolves it once
/// and hands the SCU that chain's previous hash.
/// </summary>
public enum FRReceiptChain
{
    /// <summary>Sales receipts (tickets).</summary>
    Ticket,

    /// <summary>Payment proofs (justificatifs de paiement).</summary>
    PaymentProof,

    /// <summary>Invoices (factures).</summary>
    Invoice,

    /// <summary>Grand totals: zero receipt, closings and the lifecycle receipts.</summary>
    GrandTotal,

    /// <summary>Non-fiscal documents handed to the customer: bills, pro formas, delivery notes.</summary>
    Bill,

    /// <summary>Technical event and accounting logs.</summary>
    Log,

    /// <summary>Duplicates of an already issued document.</summary>
    Duplicate,

    /// <summary>Training mode, kept strictly apart from the fiscal chains.</summary>
    Training,
}

public static class FRReceiptChainExt
{
    /// <summary>The letter the national numbering of this chain is prefixed with.</summary>
    public static string Identifier(this FRReceiptChain chain) => chain switch
    {
        FRReceiptChain.Ticket => "T",
        FRReceiptChain.PaymentProof => "P",
        FRReceiptChain.Invoice => "I",
        FRReceiptChain.GrandTotal => "G",
        FRReceiptChain.Bill => "B",
        FRReceiptChain.Log => "L",
        FRReceiptChain.Duplicate => "D",
        FRReceiptChain.Training => "X",
        _ => "T",
    };

    /// <summary>
    /// Resolves the chain a receipt is signed into. Training receipts never enter a fiscal chain,
    /// so the flag wins over the case.
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

        if (request.ftReceiptCase.IsType(ReceiptCaseType.DailyOperations) || request.ftReceiptCase.IsType(ReceiptCaseType.Lifecycle))
        {
            return FRReceiptChain.GrandTotal;
        }

        if (request.ftReceiptCase.IsType(ReceiptCaseType.Log))
        {
            return receiptCase == ReceiptCase.CopyReceiptPrintExistingReceipt0x3010 ? FRReceiptChain.Duplicate : FRReceiptChain.Log;
        }

        return receiptCase switch
        {
            ReceiptCase.PaymentTransfer0x0002 => FRReceiptChain.PaymentProof,
            ReceiptCase.DeliveryNote0x0005 => FRReceiptChain.Bill,
            (ReceiptCase) 0x0006 /* Table Check */ => FRReceiptChain.Bill,
            (ReceiptCase) 0x0007 /* Pro Forma */ => FRReceiptChain.Bill,
            _ => FRReceiptChain.Ticket,
        };
    }
}

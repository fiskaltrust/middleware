namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// The accumulated totals of one period, broken down the way NF525 grand-total receipts report
/// them: the turnover split by VAT class and the settlement split by payment nature.
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class FRTotals
{
    public decimal Totalizer { get; set; }

    public decimal CINormal { get; set; }

    public decimal CIReduced1 { get; set; }

    public decimal CIReduced2 { get; set; }

    public decimal CIReducedS { get; set; }

    public decimal CIZero { get; set; }

    public decimal CIUnknown { get; set; }

    public decimal PICash { get; set; }

    public decimal PINonCash { get; set; }

    public decimal PIInternal { get; set; }

    public decimal PIUnknown { get; set; }
}

/// <summary>
/// The periods a French closing receipt reports. Each accumulates independently and is reset by
/// its own closing, so a monthly closing does not depend on the daily ones having been sent.
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public enum FRTotalsPeriod
{
    Shift,
    Day,
    Month,
    Year,

    /// <summary>The grand total since the queue was opened. Never reset.</summary>
    Perpetual,
}

/// <summary>
/// The totals a grand-total (closing) receipt carries. A closing request has no charge or pay
/// items of its own - what it attests is the accumulated turnover of the period, so the queue
/// computes it and the SCU signs it alongside the receipt.
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>.</remarks>
public class FRPeriodTotals
{
    /// <summary>The period this closing reports and resets.</summary>
    public FRTotalsPeriod Period { get; set; }

    /// <summary>The totals of <see cref="Period"/> since its previous closing.</summary>
    public FRTotals Current { get; set; } = new();

    /// <summary>The grand total since the queue was opened; reported on every closing.</summary>
    public FRTotals Perpetual { get; set; } = new();
}

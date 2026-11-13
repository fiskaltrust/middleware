namespace fiskaltrust.ifPOS.v2.fr;

/// <summary>
/// One period a closing or the queue stop closes, and where its totals started: the previous closing of that period,
/// else the queue's start (archive-v2 §1.3 <c>PeriodStart</c>). It ends at the receipt's own moment. A monthly closing
/// closes the day and the month, a yearly closing and the queue stop the day, the month and the year (market-fr#221, as
/// v1.2).
/// </summary>
/// <remarks>Temporary local declaration, see <see cref="IFRSSCD"/>. The queue hands it over in <c>ftStateData.FR.Closing.Periods</c>.</remarks>
public class FRClosedPeriod
{
    /// <summary><c>Shift</c>, <c>Day</c>, <c>Month</c> or <c>Year</c>.</summary>
    public FRTotalsPeriod Period { get; set; }

    public DateTime Start { get; set; }
}

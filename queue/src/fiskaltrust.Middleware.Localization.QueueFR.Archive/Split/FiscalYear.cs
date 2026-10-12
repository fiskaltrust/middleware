using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Split
{
    /// <summary>
    /// The start of a fiscal year as a month and day. An archive covers at most one year or one fiscal
    /// year (NF525 V2.3 §6.9.2.3, LNE Exigence 11), so the split cuts every requested period at these
    /// boundaries (market-fr#146).
    /// </summary>
    public sealed class FiscalYear
    {
        /// <summary>Fiscal year = calendar year (1 January).</summary>
        public static readonly FiscalYear Calendar = new FiscalYear(1, 1);

        public FiscalYear(int startMonth, int startDay)
        {
            if (startMonth < 1 || startMonth > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(startMonth));
            }
            // Capped at 28 so that every year has the start day (no 29 February or 31st problems).
            if (startDay < 1 || startDay > 28)
            {
                throw new ArgumentOutOfRangeException(nameof(startDay), "A fiscal year must start on day 1 to 28 of a month.");
            }
            StartMonth = startMonth;
            StartDay = startDay;
        }

        public int StartMonth { get; }

        public int StartDay { get; }

        /// <summary>The start (local wall-clock time, unspecified kind) of the fiscal year containing <paramref name="localMoment"/>.</summary>
        public DateTime StartOfYearContaining(DateTime localMoment)
        {
            var candidate = new DateTime(localMoment.Year, StartMonth, StartDay, 0, 0, 0, DateTimeKind.Unspecified);
            return localMoment < candidate ? candidate.AddYears(-1) : candidate;
        }

        public override string ToString() => $"--{StartMonth:00}-{StartDay:00}";
    }
}

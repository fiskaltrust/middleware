namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

/// <summary>
/// Helper class for converting UTC times to Portugal local time
/// </summary>
public static class PortugalTimeHelper
{
    // Portugal uses Western European Time (WET) / Western European Summer Time (WEST)
    // TimeZoneInfo ID for Portugal: "GMT Standard Time" on Windows, "Europe/Lisbon" on Linux/Mac
    private static readonly TimeZoneInfo _portugalTimeZone = GetPortugalTimeZone();

    private static TimeZoneInfo GetPortugalTimeZone()
    {
        try
        {
            // Try Windows time zone ID first
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Try IANA time zone ID (Linux/Mac)
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: create custom time zone for Portugal (UTC+0/+1 for DST)
                var adjustmentRules = new[]
                {
                    // DST rule for Portugal: Last Sunday of March to Last Sunday of October
                    TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                        DateTime.MinValue.Date,
                        DateTime.MaxValue.Date,
                        TimeSpan.FromHours(1),
                        TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 3, 5, DayOfWeek.Sunday),
                        TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 5, DayOfWeek.Sunday)
                    )
                };
                return TimeZoneInfo.CreateCustomTimeZone("Portugal Time", TimeSpan.Zero, "Portugal Time", "WET", "WEST", adjustmentRules);
            }
        }
    }

    /// <summary>
    /// Converts UTC DateTime to Portugal local time
    /// </summary>
    /// <param name="utcDateTime">The UTC DateTime to convert</param>
    /// <returns>The DateTime in Portugal local time</returns>
    public static DateTime ConvertToPortugalTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            // If not UTC, assume it's already in the correct time zone or convert to UTC first
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _portugalTimeZone);
    }
}

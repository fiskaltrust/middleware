namespace fiskaltrust.Middleware.SCU.PL.TestSupport;

/// <summary>
/// The committed business-case files are a contract between two consumers — the end-to-end suite and
/// the test launcher, which links the very same folder — so the placeholders in them are resolved in
/// one place rather than once per consumer.
/// </summary>
/// <remarks>
/// <c>cbReceiptReference</c> is deliberately not one of them: the cases reference each other (the
/// return receipt points at the cash sale), which only works with the references as written.
/// </remarks>
public static class BusinessCaseSample
{
    public const string CashBoxIdPlaceholder = "{{ ftCashBoxID }}";

    public const string PosSystemIdPlaceholder = "{{ ftPosSystemID }}";

    /// <summary>The business case with this run's cashbox and pos system filled in.</summary>
    public static string Resolve(string rawJson, Guid cashBoxId, Guid posSystemId)
    {
        ArgumentNullException.ThrowIfNull(rawJson);
        return rawJson
            .Replace(CashBoxIdPlaceholder, cashBoxId.ToString(), StringComparison.Ordinal)
            .Replace(PosSystemIdPlaceholder, posSystemId.ToString(), StringComparison.Ordinal);
    }
}

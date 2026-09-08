using System.Globalization;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Emulator;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;

/// <summary>
/// The register's counters and totalizers at one moment, read with <c>stot</c> and <c>scnt</c>.
/// Two of these, taken around a receipt, say what the receipt changed on the device.
/// </summary>
/// <param name="NextDailyReportNumber"><c>no</c> — the number the next daily report will get.</param>
/// <param name="ReceiptTotalizersGrosze"><c>pa..pg</c> — what was sold in each PTU slot since the last daily report.</param>
/// <param name="ReceiptCount"><c>pn</c> — receipts since the last daily report.</param>
/// <param name="CanceledCount"><c>cn</c> — canceled receipts.</param>
/// <param name="CanceledTotalGrosze"><c>ct</c> — the value of the canceled receipts.</param>
/// <param name="LastReceiptNumber"><c>bt</c> — the number of the last receipt, which the SCU reports as the fiscal document number.</param>
/// <param name="CompletedReceipts"><c>bn</c> — correctly completed receipts.</param>
/// <param name="NonFiscalPrintouts"><c>nf</c> — non-fiscal printouts, among them goods returns.</param>
public sealed record FiscalSnapshot(
    int NextDailyReportNumber,
    IReadOnlyList<long> ReceiptTotalizersGrosze,
    int ReceiptCount,
    int CanceledCount,
    long CanceledTotalGrosze,
    int LastReceiptNumber,
    int CompletedReceipts,
    int NonFiscalPrintouts)
{
    public static FiscalSnapshot From(PosNetResponse totalizers, PosNetResponse counters)
    {
        ArgumentNullException.ThrowIfNull(totalizers);
        ArgumentNullException.ThrowIfNull(counters);
        return new FiscalSnapshot(
            NextDailyReportNumber: DeviceFields.Int(totalizers, "no"),
            ReceiptTotalizersGrosze: DeviceFields.PerSlotGrosze(totalizers, 'p'),
            ReceiptCount: DeviceFields.Int(totalizers, "pn"),
            CanceledCount: DeviceFields.Int(totalizers, "cn"),
            CanceledTotalGrosze: DeviceFields.Grosze(totalizers, "ct"),
            LastReceiptNumber: DeviceFields.Int(counters, "bt"),
            CompletedReceipts: DeviceFields.Int(counters, "bn"),
            NonFiscalPrintouts: DeviceFields.Int(totalizers, "nf"));
    }
}

/// <summary>
/// What <c>strns</c> reports: the transaction state and the totalizers of the receipt in progress —
/// or, once <c>trend</c> has closed it, of the receipt just completed, since the register resets
/// them only when the next transaction starts (POT-I-DEV-05 p.259).
/// </summary>
/// <param name="TransactionOpen"><c>to</c>.</param>
/// <param name="DocumentType"><c>ts</c> — 16 for a receipt, 33 for an invoice, 0 outside a transaction after a RAM reset.</param>
/// <param name="PerRateGrosze"><c>va..vg</c> — the receipt's value per PTU slot.</param>
/// <param name="PaymentsGrosze"><c>fp</c> — what was paid.</param>
/// <param name="ChangeGrosze"><c>re</c> — the change handed out.</param>
public sealed record TransactionReading(
    bool TransactionOpen,
    int DocumentType,
    IReadOnlyList<long> PerRateGrosze,
    long PaymentsGrosze,
    long ChangeGrosze)
{
    public long TotalGrosze => PerRateGrosze.Sum();

    public static TransactionReading From(PosNetResponse transactionStatus)
    {
        ArgumentNullException.ThrowIfNull(transactionStatus);
        return new TransactionReading(
            TransactionOpen: DeviceFields.Int(transactionStatus, "to") == 1,
            DocumentType: DeviceFields.Int(transactionStatus, "ts"),
            PerRateGrosze: DeviceFields.PerSlotGrosze(transactionStatus, 'v'),
            PaymentsGrosze: DeviceFields.Grosze(transactionStatus, "fp"),
            ChangeGrosze: DeviceFields.Grosze(transactionStatus, "re"));
    }
}

/// <summary>
/// Reads the fields of a status response. Amounts come as integer grosze off the device
/// (<c>pa1260</c> recorded for 12.60 PLN); the <c>12,34</c> form the specification's examples
/// use for rates is accepted for amounts as well, rounded to grosze the way the SCU rounds
/// (<see cref="PLAmountExtensions.ToGrosze"/>). A field the device did not send reads as zero —
/// the specification marks most of them optional.
/// </summary>
internal static class DeviceFields
{
    public static int Int(PosNetResponse response, string key)
        => response.Parameters.TryGetValue(key, out var text)
           && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    public static long Grosze(PosNetResponse response, string key)
        => response.Parameters.TryGetValue(key, out var text) ? ParseGrosze(text) : 0;

    public static long[] PerSlotGrosze(PosNetResponse response, char prefix)
        => Enumerable.Range(0, PosNetDeviceModel.SlotCount).Select(i => Grosze(response, $"{prefix}{(char)('a' + i)}")).ToArray();

    public static long ParseGrosze(string text)
    {
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var grosze))
        {
            return grosze;
        }
        return decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var pln)
            ? pln.ToGrosze()
            : throw new FormatException($"'{text}' is not a number the way a POSNET register writes one.");
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Writing;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// One journal of the archive (archive-v2 field contract §7, market-fr#173): the journal code a POS
    /// asks for, the file it lands in, and the chain letters whose entries it holds.
    /// </summary>
    public sealed class ArchiveJournal
    {
        internal ArchiveJournal(int code, string letter, string description, params string[] chainLetters)
        {
            Code = code;
            Letter = letter;
            Description = description;
            ChainLetters = new HashSet<string>(chainLetters, StringComparer.Ordinal);
        }

        /// <summary>The <c>jjj</c> part of the <c>ftJournalType</c>, e.g. 0x001 for tickets.</summary>
        public int Code { get; }

        /// <summary>The contract's letter, which names the file.</summary>
        public string Letter { get; }

        public string Description { get; }

        /// <summary>
        /// The letters the queue writes into <c>ftReceiptIdentification</c> for this journal: the journal's own
        /// <see cref="Letter"/>, since every ADR 0002 chain has its own journal.
        /// </summary>
        public IReadOnlyCollection<string> ChainLetters { get; }

        public string FileName => $"journal-{Letter}.csv";

        public bool Holds(ArchiveEntry entry) => ChainLetters.Contains(entry.Chain);
    }

    /// <summary>
    /// The files of a v2 sub-archive (ADR 0006, archive-v2 field contract, market-fr#227). This is the first
    /// slice of market-fr#199: the columns the stored queue item already carries.
    /// </summary>
    /// <remarks>
    /// Not written yet, because their source is still open or not stored yet: the issuer claims (#141),
    /// <c>SoftwareVersion</c> (#124), the totals and counters of §1.2–§1.4, the response-computed charge-item
    /// values (<c>ftStateData.FR.ChargeItems[]</c>, D7), <c>receipt-vat-summary.csv</c> (§4, #147),
    /// <c>signatures.csv</c> (§5) and <c>jet.csv</c> (§6, #200). The per-SCU schema (D1) is not applied:
    /// both schemas' common columns are written.
    /// </remarks>
    public static class ArchiveV2Files
    {
        public static readonly IReadOnlyList<ArchiveJournal> Journals = new[]
        {
            new ArchiveJournal(0x001, "T", "Ticket", "T"),
            new ArchiveJournal(0x002, "P", "Payment proof", "P"),
            new ArchiveJournal(0x003, "I", "Invoice", "I"),
            new ArchiveJournal(0x004, "E", "Technical", "E"),
            new ArchiveJournal(0x007, "B", "Bill (note)", "B"),
            new ArchiveJournal(0x008, "A", "Archive", "A"),
            new ArchiveJournal(0x009, "L", "Log", "L"),
            new ArchiveJournal(0x00A, "D", "Copy", "D"),
            new ArchiveJournal(0x00B, "X", "Training", "X"),
            new ArchiveJournal(0x00C, "S", "Shift closing", "S"),
            new ArchiveJournal(0x00D, "Z", "Daily closing", "Z"),
            new ArchiveJournal(0x00E, "M", "Monthly closing", "M"),
            new ArchiveJournal(0x00F, "Y", "Yearly closing", "Y"),
        };

        /// <summary>
        /// One anchor per journal file with rows in <paramref name="subArchive"/> (market-fr#262, contract section 10):
        /// <c>chain</c> is the journal letter, <c>firstReceiptIdentification</c> its first row, and <c>previousSignature</c>
        /// exactly what that row's <c>PreviousSignature</c> column holds (the Base64Url signature part, empty at a chain's start).
        /// </summary>
        public static IReadOnlyList<ManifestChainAnchor> ChainAnchorsOf(SubArchive<ArchiveEntry> subArchive)
            => Journals
                .Select(journal => (journal, first: subArchive.Records.FirstOrDefault(journal.Holds)))
                .Where(x => x.first is not null)
                .Select(x => new ManifestChainAnchor { Chain = x.journal.Letter, FirstReceiptIdentification = x.first!.ReceiptIdentification, PreviousSignature = x.first.PreviousSignature ?? "" })
                .ToList();

        /// <summary>
        /// The column schema of <paramref name="subArchive"/>: <c>LNE</c> when its receipts sign a delimited data set (LNE and
        /// self-certified SCUs), <c>InfoCert</c> when they sign a JWS; null for a sub-archive without receipts.
        /// </summary>
        public static string? SchemaOf(SubArchive<ArchiveEntry> subArchive)
            => subArchive.Records.Count == 0 ? null : subArchive.Records[0].Signature.Contains('|') ? "LNE" : "InfoCert";

        public const string ChargeItemsFileName = "charge-items.csv";
        public const string PayItemsFileName = "pay-items.csv";
        public const string JournalTypesFileName = "journal-types.csv";
        public const string CaseDataFileName = "case-data.csv";

        private const long PayItemDownpaymentFlag = 0x0008_0000;

        private static readonly HashSet<string> ChargeItemChains = new HashSet<string>(new[] { "T", "I", "B", "X" }, StringComparer.Ordinal);
        private static readonly HashSet<string> PayItemChains = new HashSet<string>(new[] { "T", "P", "I", "B", "X" }, StringComparer.Ordinal);

        public static ArchiveJournal? JournalOf(int code) => Journals.FirstOrDefault(j => j.Code == code);

        /// <summary>The sub-archive an entry belongs to: its SCU certificate, format version 2 (ADR 0005).</summary>
        public static SubArchiveKey KeyOf(ArchiveEntry entry)
            => new SubArchiveKey(string.IsNullOrEmpty(entry.CertificateSerialNumber) ? "unknown" : entry.CertificateSerialNumber!, ArchiveFormatVersion.V2);

        /// <summary>The journal file of one journal, e.g. for a single-journal export.</summary>
        public static ICsvFileDefinition<ArchiveEntry> JournalFile(ArchiveJournal journal)
            => new FlatCsvFile<ArchiveEntry>(journal.FileName, CommonColumns, journal.Holds);

        public static IReadOnlyList<ICsvFileDefinition<ArchiveEntry>> Files()
            => Journals.Select(JournalFile)
                .Concat(new ICsvFileDefinition<ArchiveEntry>[]
                {
                    new OneToManyCsvFile<ArchiveEntry, JsonElement>(ChargeItemsFileName, e => ArchiveEntry.Array(e.Request, "cbChargeItems"), ChargeItemColumns, e => ChargeItemChains.Contains(e.Chain)),
                    new OneToManyCsvFile<ArchiveEntry, JsonElement>(PayItemsFileName, e => ArchiveEntry.Array(e.Request, "cbPayItems"), PayItemColumns, e => PayItemChains.Contains(e.Chain)),
                    new JournalTypesCsvFile(),
                    new OneToManyCsvFile<ArchiveEntry, CaseDataRow>(CaseDataFileName, CaseData.Rows, CaseDataColumns),
                    new RequestsFile(),
                })
                .ToList();

        public static ISubArchiveWriter<ArchiveEntry> CreateWriter(CsvOptions? options = null)
            => new CsvSubArchiveWriter<ArchiveEntry>(ArchiveFormatVersion.V2, Files(), options);

        /// <summary>§1.1, the columns common to every chain.</summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> CommonColumns = new[]
        {
            new CsvColumn<ArchiveEntry>("ftQueueItemID", e => e.QueueItemId),
            new CsvColumn<ArchiveEntry>("ftQueueID", e => e.QueueId),
            new CsvColumn<ArchiveEntry>("ftCashBoxIdentification", e => ArchiveEntry.String(e.Response, "ftCashBoxIdentification")),
            new CsvColumn<ArchiveEntry>("ftQueueRow", e => e.QueueRow),
            new CsvColumn<ArchiveEntry>("Chain", e => e.Chain),
            new CsvColumn<ArchiveEntry>("ftReceiptIdentification", e => e.ReceiptIdentification),
            new CsvColumn<ArchiveEntry>("ChainCounter", e => e.ChainCounter),
            new CsvColumn<ArchiveEntry>("StartOfChain", e => e.StartOfChain),
            new CsvColumn<ArchiveEntry>("PreviousSignature", e => e.PreviousSignature),
            new CsvColumn<ArchiveEntry>("ftReceiptMoment", e => e.ReceiptMoment),
            new CsvColumn<ArchiveEntry>("cbReceiptMoment", e => MomentOrText(e.Request, "cbReceiptMoment")),
            new CsvColumn<ArchiveEntry>("cbReceiptReference", e => ArchiveEntry.String(e.Request, "cbReceiptReference")),
            new CsvColumn<ArchiveEntry>("cbPreviousReceiptReference", e => References(e.Request, "cbPreviousReceiptReference")),
            new CsvColumn<ArchiveEntry>("ftReceiptCase", e => CsvValueFormatter.Hex(e.ReceiptCase)),
            new CsvColumn<ArchiveEntry>("cbTerminalID", e => ArchiveEntry.Text(e.Request, "cbTerminalID")),
            new CsvColumn<ArchiveEntry>("cbUser", e => ArchiveEntry.Text(e.Request, "cbUser")),
            new CsvColumn<ArchiveEntry>("cbArea", e => ArchiveEntry.Text(e.Request, "cbArea")),
            new CsvColumn<ArchiveEntry>("ftState", e => CsvValueFormatter.Hex(e.State)),
            new CsvColumn<ArchiveEntry>("RequestHash", e => e.RequestHash),
            new CsvColumn<ArchiveEntry>("Siret", e => e.Siret),
            new CsvColumn<ArchiveEntry>("CertificateSerialNumber", e => e.CertificateSerialNumber),
            new CsvColumn<ArchiveEntry>("Training", e => e.Training),
            new CsvColumn<ArchiveEntry>("Signature", e => e.Signature),
        };

        /// <summary>§2, the charge-item columns sent by the POS.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, JsonElement>>> ChargeItemColumns = new[]
        {
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ftQueueItemID", r => r.Parent.QueueItemId),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Position", r => Number(r.Child, "Position")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ProductNumber", r => ArchiveEntry.Text(r.Child, "ProductNumber")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Description", r => ArchiveEntry.Text(r.Child, "Description")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Quantity", r => Number(r.Child, "Quantity")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Unit", r => ArchiveEntry.Text(r.Child, "Unit")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("UnitPrice", r => Number(r.Child, "UnitPrice")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Amount", r => Number(r.Child, "Amount")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("VATRate", r => Number(r.Child, "VATRate")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ftChargeItemCase", r => Hex(r.Child, "ftChargeItemCase")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Moment", r => MomentOrText(r.Child, "Moment")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("DiscountCode", r => CaseDataFR(r.Child, "ftChargeItemCaseData", "DiscountCode")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("DiscountRate", r => CaseDataFR(r.Child, "ftChargeItemCaseData", "DiscountRate")),
        };

        /// <summary>§3, the pay-item columns.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, JsonElement>>> PayItemColumns = new[]
        {
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ftQueueItemID", r => r.Parent.QueueItemId),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Position", r => Number(r.Child, "Position")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ftPayItemCase", r => Hex(r.Child, "ftPayItemCase")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Description", r => ArchiveEntry.Text(r.Child, "Description")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Amount", r => Number(r.Child, "Amount")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Quantity", r => Number(r.Child, "Quantity")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Moment", r => (object?) MomentOrText(r.Child, "Moment") ?? r.Parent.ReceiptMoment),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Currency", r => CaseDataFR(r.Child, "ftPayItemCaseData", "Currency")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("ExchangeRate", r => CaseDataFR(r.Child, "ftPayItemCaseData", "ExchangeRate")),
            new CsvColumn<ChildRow<ArchiveEntry, JsonElement>>("Downpayment", r => ArchiveEntry.Long(r.Child, "ftPayItemCase") is { } c ? (c & PayItemDownpaymentFlag) != 0 : (bool?) null),
        };

        /// <summary>§8, the last-resort copy of the case data and object-typed request fields.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>> CaseDataColumns = new[]
        {
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("ftQueueItemID", r => r.Parent.QueueItemId),
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification),
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("Owner", r => r.Child.Owner),
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("Position", r => r.Child.Position),
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("Field", r => r.Child.Field),
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("Path", r => r.Child.Path),
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("ValueType", r => r.Child.ValueType),
            new CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>("Value", r => r.Child.Value),
        };

        /// <summary>A number as the POS sent it: the stored JSON text, so no rounding or reformatting.</summary>
        private static string? Number(JsonElement element, string name)
            => ArchiveEntry.Property(element, name) is { } value && value.ValueKind == JsonValueKind.Number ? value.GetRawText() : ArchiveEntry.Text(element, name);

        private static string? Hex(JsonElement element, string name)
            => ArchiveEntry.Long(element, name) is { } value ? CsvValueFormatter.Hex(value) : null;

        private static object? MomentOrText(JsonElement element, string name)
            => (object?) ArchiveEntry.Moment(element, name) ?? ArchiveEntry.Text(element, name);

        /// <summary>One reference, or several joined by commas (v2 allows an array of previous references).</summary>
        private static string? References(JsonElement element, string name)
            => ArchiveEntry.Property(element, name) is { } value && value.ValueKind == JsonValueKind.Array
                ? string.Join(",", value.EnumerateArray().Select(v => v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText()))
                : ArchiveEntry.Text(element, name);

        /// <summary>A key of the FR country block of an item's case data (D2b).</summary>
        private static string? CaseDataFR(JsonElement item, string field, string key)
            => ArchiveEntry.Property(item, field) is { } caseData && ArchiveEntry.Property(caseData, "FR") is { } fr
                ? (ArchiveEntry.Property(fr, key) is { } value && value.ValueKind == JsonValueKind.Number ? value.GetRawText() : ArchiveEntry.Text(fr, key))
                : null;

        /// <summary>§7: the journal-type table, so a reader can map the files. It does not depend on the entries.</summary>
        private sealed class JournalTypesCsvFile : ICsvFileDefinition<ArchiveEntry>
        {
            public string FileName => JournalTypesFileName;

            public async Task WriteAsync(IReadOnlyList<ArchiveEntry> records, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
            {
                var csv = new CsvWriter(writer, options);
                if (options.WriteHeader)
                {
                    await csv.WriteRowAsync(new[] { "Code", "Chain", "Description" }, cancellationToken).ConfigureAwait(false);
                }
                var rows = new[] { new[] { "000", "", "Status information QueueFR" } }
                    .Concat(Journals.Select(j => new[] { j.Code.ToString("X3", CultureInfo.InvariantCulture), j.Letter, j.Description }))
                    .Concat(new[] { new[] { "010", "", "Export (with Archive)" } });
                foreach (var row in rows)
                {
                    await csv.WriteRowAsync(row, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}

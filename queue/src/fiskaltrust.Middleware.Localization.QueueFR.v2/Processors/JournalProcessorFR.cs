using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Split;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// FR-specific journal exports. Only journal types qualified with a country code reach this
/// processor - the international raw exports (action journal, receipt journal, queue items,
/// configuration) are served by the shared <see cref="JournalProcessor"/> and are unaffected.
/// </summary>
/// <remarks>
/// <para>
/// The exports are written by the shared archive package (ADR 0008, fiskaltrust/market-fr#218), so a
/// journal exported here has the same columns as the same journal inside a fiscal archive:
/// </para>
/// <list type="bullet">
/// <item><see cref="JournalTypeFR.StatusInformation"/>: the queue's FR identification and the journal-type table, as JSON.</item>
/// <item>The group journals (001-00B): that journal's CSV file (archive-v2 field contract §1, §7).</item>
/// <item><see cref="JournalTypeFR.ArchiveExport"/>: the signed main archive of one fiscal year (ADR 0005-0007).</item>
/// </list>
/// <para>
/// <c>From</c> and <c>To</c> are UTC ticks of the receipt moment, both inclusive; <c>To</c> 0 means
/// "up to now". Every export reads the queue items from the start, because the previous signature of
/// a chain's first entry in the period is only known from the entry before it (ADR 0001; the LNE SCU
/// stores no copy of its signed data set). Only the last signature per chain is held for the entries
/// before the period.
/// </para>
/// <para>
/// A request that cannot be answered truthfully is refused rather than answered with an empty
/// document, which would read like a queue with no records: the archive journal (008) until the queue
/// signs archive receipts (ADR 0009, fiskaltrust/market-fr#209, #244), and the archive export when the
/// queue's SCU cannot sign it (ADR 0007 amendment: no hash signing, or no configured certificate).
/// </para>
/// </remarks>
public class JournalProcessorFR : IJournalProcessor
{
    private const long CountryMask = unchecked((long) 0xFFFF_0000_0000_0000);
    private const long CountryFR = 0x4652_0000_0000_0000;
    private const int ChunkSize = 64 * 1024;

    private readonly Guid _queueId;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository;
    private readonly Func<Task<IArchiveSigner>>? _archiveSigner;
    private readonly ArchiveSplitOptions _splitOptions;

    /// <param name="archiveSigner">
    /// Creates the signer of an archive export, per export: the queue's SCU (<see cref="ScuArchiveSigner"/>, ADR 0007 amendment).
    /// Null refuses the archive export.
    /// </param>
    public JournalProcessorFR(Guid queueId, IStorageProvider storageProvider, Func<Task<IArchiveSigner>>? archiveSigner = null, ArchiveSplitOptions? splitOptions = null)
    {
        _queueId = queueId;
        _queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();
        _configurationRepository = storageProvider.CreateConfigurationRepository();
        _archiveSigner = archiveSigner;
        _splitOptions = splitOptions ?? new ArchiveSplitOptions();
    }

    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
    {
        var journalType = (long) request.ftJournalType;
        if ((journalType & CountryMask) != CountryFR)
        {
            throw new ArgumentException($"The journal type 0x{journalType:X} is not a French journal type and is not supported by the QueueFR.v2 localization.");
        }

        var journalTypeFR = request.ftJournalType.TypeFR();
        if (!journalTypeFR.IsKnown())
        {
            throw new ArgumentException($"The French journal type 0x{journalType:X} is unknown. The French journal types are listed in {nameof(JournalTypeFR)}.");
        }

        if (journalTypeFR == JournalTypeFR.StatusInformation)
        {
            return (new ContentType(MediaTypeNames.Application.Json) { CharSet = Encoding.UTF8.WebName }, StatusAsync());
        }

        if (journalTypeFR == JournalTypeFR.Archive)
        {
            throw new NotSupportedException(
                $"The French journal type 0x{journalType:X} ({journalTypeFR}) exports the archive receipts, and the QueueFR.v2 localization does not sign archive receipts yet " +
                "(fiskaltrust/market-fr#209, #244). The archive itself is exported with the ArchiveExport journal type (0x4652_2000_0000_0010).");
        }

        var (from, to) = Period(request);

        if (journalTypeFR == JournalTypeFR.ArchiveExport)
        {
            if (_archiveSigner is null)
            {
                throw new NotSupportedException(
                    $"The French journal type 0x{journalType:X} ({journalTypeFR}) needs an archive signer (ADR 0007), and none is configured for this queue. " +
                    "The per-journal exports (0x4652_2000_0000_0001 to _000B) are available.");
            }

            var period = new ArchivePeriod(from, to);
            var slices = ArchiveSplitter.Slices(period, _splitOptions);
            if (slices.Count > 1)
            {
                throw new ArgumentException(
                    $"The requested period {period} spans {slices.Count} fiscal years. An archive covers at most one fiscal year (fiskaltrust/market-fr#146): request each year on its own, starting at {slices[1].Start:o}.");
            }

            return (new ContentType("application/zip"), ArchiveAsync(period, _archiveSigner));
        }

        var journal = ArchiveV2Files.JournalOf((int) ((long) journalTypeFR & 0xFFF))
            ?? throw new InvalidOperationException($"No archive journal for {journalTypeFR}.");
        return (new ContentType("text/csv") { CharSet = Encoding.UTF8.WebName }, JournalAsync(journal, new ArchivePeriod(from, to)));
    }

    /// <summary>[From, To] in ticks, inclusive, as a half-open UTC period.</summary>
    private static (DateTime from, DateTime to) Period(JournalRequest request)
    {
        if (request.From < 0 || request.To < 0)
        {
            throw new ArgumentException("The French journals take From and To as UTC ticks of the receipt moment (To 0 = up to now); a negative To (take a number of entries) is not supported.");
        }

        var from = new DateTime(request.From, DateTimeKind.Utc);
        var to = request.To == 0 ? DateTime.UtcNow : new DateTime(request.To, DateTimeKind.Utc).AddTicks(1);
        if (to <= from)
        {
            throw new ArgumentException($"The journal period ends ({to:o}) before it starts ({from:o}).");
        }
        return (from, to);
    }

    private async IAsyncEnumerable<byte[]> StatusAsync()
    {
        var queueFR = await (await _configurationRepository).GetQueueFRAsync(_queueId).ConfigureAwait(false);

        // The v1 counters and totals of ftQueueFR are not maintained by the v2 localization, so they are
        // left out rather than reported stale.
        yield return JsonSerializer.SerializeToUtf8Bytes(new
        {
            QueueFR = queueFR is null ? null : new
            {
                queueFR.ftQueueFRId,
                queueFR.CashBoxIdentification,
                queueFR.Siret,
                queueFR.ftSignaturCreationUnitFRId,
            },
            JournalTypes = new[] { new { Code = "000", Chain = "", Description = "Status information QueueFR" } }
                .Concat(ArchiveV2Files.Journals.Select(j => new { Code = j.Code.ToString("X3"), Chain = j.Letter, j.Description }))
                .Concat(new[] { new { Code = "010", Chain = "", Description = "Export (with Archive)" } }),
        });
    }

    private async IAsyncEnumerable<byte[]> JournalAsync(ArchiveJournal journal, ArchivePeriod period, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entries = await ReadEntriesAsync(period, rejectV1: false).ConfigureAwait(false);
        var file = ArchiveV2Files.JournalFile(journal);

        await foreach (var chunk in ThroughTempFileAsync(async stream =>
        {
            var options = new CsvOptions();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), ChunkSize, leaveOpen: true) { NewLine = options.NewLine };
            await file.WriteAsync(entries, writer, options, cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<byte[]> ArchiveAsync(ArchivePeriod period, Func<Task<IArchiveSigner>> createSigner)
    {
        var signer = await createSigner().ConfigureAwait(false);
        var entries = await ReadEntriesAsync(period, rejectV1: true).ConfigureAwait(false);
        var archive = ArchiveSplitter.Split(entries, e => e.ReceiptMoment, ArchiveV2Files.KeyOf, period, _splitOptions).Single();

        var queueFR = await (await _configurationRepository).GetQueueFRAsync(_queueId).ConfigureAwait(false);
        var siret = (string.IsNullOrEmpty(queueFR?.Siret) ? null : queueFR!.Siret)
            ?? entries.Select(e => e.Siret).FirstOrDefault(s => !string.IsNullOrEmpty(s))
            ?? throw new InvalidOperationException("The archive manifest needs the SIRET, and neither the queue configuration nor the archived entries carry one.");
        // When the SCU signs the archive, its certificate is also the one of the entries it signed: embed it in their sub-archive.
        var scuCertificate = signer is ScuArchiveSigner scuSigner
            ? (Func<SubArchiveKey, X509Certificate2?>) (key => key.CertificateSerialNumber == scuSigner.CertificateSerialNumber ? scuSigner.Certificate : null)
            : null;
        var packager = new ArchivePackager<ArchiveEntry>(new[] { ArchiveV2Files.CreateWriter() }, signer, scuCertificate);

        await foreach (var chunk in ThroughTempFileAsync(stream => packager.WriteAsync(archive, new ArchiveMetadata(siret, DateTime.UtcNow), stream)).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// The signed v2 entries whose receipt moment falls in <paramref name="period"/>, in queue-row order,
    /// chained on the entries before them.
    /// </summary>
    private async Task<List<ArchiveEntry>> ReadEntriesAsync(ArchivePeriod period, bool rejectV1)
    {
        var repository = await _queueItemRepository;
        var linker = new ArchiveChainLinker();
        var entries = new List<ArchiveEntry>();

        // Linking needs queue-row order; the storage returns items by timestamp, which a sequential queue
        // writes in the same order. A misordered pair would leave PreviousSignature empty, never wrong.
        await foreach (var queueItem in repository.GetEntriesOnOrAfterTimeStampAsync(0).ConfigureAwait(false))
        {
            var entry = ArchiveEntry.TryCreate(queueItem.ftQueueItemId, queueItem.ftQueueId, queueItem.ftQueueRow, queueItem.request, queueItem.response, queueItem.requestHash);
            if (entry is null)
            {
                if (rejectV1 && period.Contains(queueItem.ftQueueMoment) && IsV1Entry(queueItem))
                {
                    throw new NotSupportedException(
                        $"The period {period} holds entries of the v1 QueueFR (queue row {queueItem.ftQueueRow}). Their sub-archive format (ADR 0005) is not written by the QueueFR.v2 localization yet (fiskaltrust/market-fr#192): request the period from the first v2 entry on.");
                }
                continue;
            }

            linker.Link(entry);
            if (period.Contains(entry.ReceiptMoment))
            {
                entries.Add(entry);
            }
        }

        return entries.OrderBy(e => e.QueueRow).ToList();
    }

    /// <summary>A v1 FR entry: signed with v1 signature types (<c>0x4652_0000_…</c>, no v2 format nibble).</summary>
    private static bool IsV1Entry(ftQueueItem queueItem)
    {
        if (string.IsNullOrEmpty(queueItem.response))
        {
            return false;
        }
        try
        {
            using var document = JsonDocument.Parse(queueItem.response);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("ftSignatures", out var signatures)
                && signatures.ValueKind == JsonValueKind.Array
                && signatures.EnumerateArray().Any(s => s.ValueKind == JsonValueKind.Object
                    && s.TryGetProperty("ftSignatureType", out var type)
                    && type.ValueKind == JsonValueKind.Number && type.TryGetInt64(out var value)
                    && (value & unchecked((long) 0xFFFF_F000_0000_0000)) == CountryFR);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Writes the export to a temporary file and streams it back in chunks, so a year of entries is
    /// never held as one byte array.
    /// </summary>
    private static async IAsyncEnumerable<byte[]> ThroughTempFileAsync(Func<Stream, Task> write)
    {
        var path = Path.GetTempFileName();
        try
        {
            await using (var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, ChunkSize, useAsync: true))
            {
                await write(output).ConfigureAwait(false);
            }

            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize, useAsync: true);
            var buffer = new byte[ChunkSize];
            int read;
            while ((read = await input.ReadAsync(buffer).ConfigureAwait(false)) > 0)
            {
                yield return buffer.AsSpan(0, read).ToArray();
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}

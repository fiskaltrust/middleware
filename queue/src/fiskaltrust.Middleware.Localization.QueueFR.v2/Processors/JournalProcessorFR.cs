using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
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
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.Base.Interface;

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
/// <item><see cref="JournalTypeFR.ArchiveExport"/>: one standalone signed archive file of the requested period, at most a year under one certificate (ADR 0019).</item>
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
    private readonly Func<Task<FRSSCDInfo?>>? _scuInfo;
    private readonly string? _validationLevel;
    private readonly FRChainStateProvider? _chains;
    private readonly bool _sandbox;
    private readonly AsyncLazy<IMiddlewareActionJournalRepository> _actionJournalRepository;

    /// <param name="archiveSigner">
    /// Creates the signer of an archive export, per export: the queue's SCU (<see cref="ScuArchiveSigner"/>, ADR 0007 amendment).
    /// Null refuses the archive export.
    /// </param>
    /// <param name="scuInfo">The SCU's non-secret configuration, joined to the archive as compliance parameters (archive-v2 §12).</param>
    /// <param name="validationLevel">The queue's <c>ValidationLevel</c> as configured, or null when it is not configured (FR then applies <c>Error</c>).</param>
    /// <param name="chains">
    /// The queue's chain state, through which a defect the archive export finds in its chains is raised as a pending JET
    /// event (ADR 0009, #208). Null leaves the export unchecked.
    /// </param>
    public JournalProcessorFR(Guid queueId, IStorageProvider storageProvider, Func<Task<IArchiveSigner>>? archiveSigner = null, Func<Task<FRSSCDInfo?>>? scuInfo = null, string? validationLevel = null,
        FRChainStateProvider? chains = null, bool sandbox = false)
    {
        _chains = chains;
        _sandbox = sandbox;
        _actionJournalRepository = storageProvider.CreateMiddlewareActionJournalRepository();
        _scuInfo = scuInfo;
        _queueId = queueId;
        _queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();
        _configurationRepository = storageProvider.CreateConfigurationRepository();
        _validationLevel = validationLevel;
        _archiveSigner = archiveSigner;
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

        var (from, to) = Period(request);

        if (journalTypeFR == JournalTypeFR.ArchiveExport)
        {
            if (_archiveSigner is null)
            {
                throw new NotSupportedException(
                    $"The French journal type 0x{journalType:X} ({journalTypeFR}) needs an archive signer (ADR 0007), and none is configured for this queue. " +
                    "The per-journal exports (0x4652_2000_0000_0001 to _000F) are available.");
            }

            var period = new ArchivePeriod(from, to);
            var contentType = new ContentType("application/zip");
            return (contentType, ArchiveAsync(period, _archiveSigner, contentType));
        }

        var journal = ArchiveV2Files.JournalOf((int) ((long) journalTypeFR & 0xFFF))
            ?? throw new InvalidOperationException($"No archive journal for {journalTypeFR}.");
        return (new ContentType("text/csv") { CharSet = Encoding.UTF8.WebName }, JournalAsync(journal, journalTypeFR, new ArchivePeriod(from, to)));
    }

    /// <summary>[From, To] in ticks, inclusive, as a half-open UTC period.</summary>
    private static (DateTime from, DateTime to) Period(JournalRequest request)
    {
        if (request.From < 0 || request.To < 0)
        {
            throw new ArgumentException("The French journals take From and To as UTC ticks of the receipt moment (To 0 = up to now); a negative To (take a number of entries) is not supported.");
        }

        var from = new DateTime(request.From, DateTimeKind.Utc);
        // To is inclusive; DateTime.MaxValue stands for the open end.
        var to = request.To == 0 ? DateTime.UtcNow
            : request.To >= DateTime.MaxValue.Ticks ? DateTime.MaxValue
            : new DateTime(request.To, DateTimeKind.Utc).AddTicks(1);
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

    private async IAsyncEnumerable<byte[]> JournalAsync(ArchiveJournal journal, JournalTypeFR journalType, ArchivePeriod period, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var read = await ReadEntriesAsync(period, rejectV1: false).ConfigureAwait(false);
        var entries = ArchiveSplitter.Select(read.Entries, e => e.ReceiptMoment, period);
        var file = ArchiveV2Files.JournalFile(journal);

        await foreach (var chunk in ThroughTempFileAsync(async stream =>
        {
            var options = new CsvOptions();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), ChunkSize, leaveOpen: true) { NewLine = options.NewLine };
            await file.WriteAsync(entries, writer, options, cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            // A journal with no entry in the period exported no data.
            if (entries.Any(journal.Holds))
            {
                await RaiseExportEventAsync(journalType, period, journal.FileName).ConfigureAwait(false);
            }
        }).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<byte[]> ArchiveAsync(ArchivePeriod period, Func<Task<IArchiveSigner>> createSigner, ContentType contentType)
    {
        // Everything that can refuse the export cheaply is checked before the period is read.
        var signer = await createSigner().ConfigureAwait(false);
        var queueFR = await (await _configurationRepository).GetQueueFRAsync(_queueId).ConfigureAwait(false);
        var metadata = string.IsNullOrEmpty(queueFR?.Siret) ? null : new ArchiveMetadata(queueFR!.Siret, DateTime.UtcNow);

        using var read = await ReadEntriesAsync(period, rejectV1: true).ConfigureAwait(false);
        var files = ArchiveSplitter.Split(read.Entries, e => e.ReceiptMoment, ArchiveV2Files.KeyOf, period);
        if (files.Count == 0)
        {
            throw new ArgumentException($"The requested period {period} holds no receipt, so it gives no archive file (ADR 0019).");
        }

        // Each file is signed by the SCU that signed its entries (market-fr#262, ADR 0019). The device holds only its
        // current SCU's key: an earlier SCU's period is exported from the portal, which holds every SCU's key, so a
        // local export that reaches into it fails before anything is written. An injected signer signs every file.
        if (signer is ScuArchiveSigner scuSigner && files.FirstOrDefault(f => f.Key.CertificateSerialNumber != scuSigner.CertificateSerialNumber) is { } foreign)
        {
            throw new NotSupportedException(
                $"The period {period} holds entries signed by the SCU certificate {foreign.Key.CertificateSerialNumber} ({foreign.Period}), and this queue only holds the key of its current SCU ({scuSigner.CertificateSerialNumber}). " +
                "Export that period from the portal (fiskaltrust/market-fr#262, #177).");
        }
        if (files.Count > 1)
        {
            // A local export answers with one standalone file; the portal generates every file of a longer request.
            throw new ArgumentException(
                $"The requested period {period} gives {files.Count} archive files, and a local export delivers one (ADR 0019): request each file's period on its own: " +
                string.Join("; ", files.Select(f => $"part {f.Part} {f.Period}, closed by {f.ClosedBy.ToManifestValue()}")) + ".");
        }
        var file = files[0];
        await RaiseIntegrityDefectsAsync(file.Records).ConfigureAwait(false);

        metadata ??= new ArchiveMetadata(file.Records.Select(e => e.Siret).FirstOrDefault(s => !string.IsNullOrEmpty(s))
            ?? throw new InvalidOperationException("The archive manifest needs the SIRET, and neither the queue configuration nor the archived entries carry one."), DateTime.UtcNow);
        var context = new ArchiveV2Context { ComplianceParameters = await ComplianceParametersAsync(queueFR).ConfigureAwait(false) };
        var packager = ArchiveV2Files.CreatePackager(_ => signer, context);

        // The shared journal processor writes the whole export before it hands the content type over, so the name is set
        // when the caller reads it.
        contentType.Name = ArchivePackager<ArchiveEntry>.FileName(file);
        await foreach (var chunk in ThroughTempFileAsync(async stream =>
        {
            await packager.WriteAsync(file, metadata, stream).ConfigureAwait(false);
            await RaiseExportEventAsync(JournalTypeFR.ArchiveExport, period, contentType.Name).ConfigureAwait(false);
        }).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// A local export of fiscal data raises JET 110 (R19 "Exportation de données") as a pending event (ADR 0009, contract
    /// §6, decision of 2026-11-12), returned on the next non-training 0x2000 (Zero receipt): the archive export
    /// (0x4652_2000_0000_0010) and every journal export (0x4652_2000_0000_0001 to _000F), the L log journal, which is the
    /// JET itself, included; R19 counts any production of a data file. Raised once the file is written, so a refused
    /// request (several files, a foreign certificate, an empty period) and a journal export with no entry raise nothing.
    /// The status information (0x4652_2000_0000_0000) holds no fiscal data and raises nothing. The portal archive's
    /// trace stays with the integrator (ADR 0019).
    /// </summary>
    private async Task RaiseExportEventAsync(JournalTypeFR journalType, ArchivePeriod period, string? fileName)
    {
        if (_chains is null)
        {
            return;
        }

        await _chains.RaisePendingJetEventAsync(await _actionJournalRepository, FRJetItems.DataExport, FRJetItems.DataExportLabel, FRJetItems.Information(
            ("ftJournalType", $"0x{(long) journalType:X}"),
            ("file", fileName),
            ("from", FRJetItems.Moment(period.Start)),
            ("to", period.End == DateTime.MaxValue ? null : FRJetItems.Moment(period.End)))).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks the chains of the exported entries (<see cref="FRChainIntegrity"/>) and raises each defect code once as a
    /// pending JET event (ADR 0009, #208): the export is reported on, never refused. Only the links inside the file are
    /// checked; its last entries are not compared with the chain state, which has moved on since a past period.
    /// </summary>
    private async Task RaiseIntegrityDefectsAsync(IEnumerable<ArchiveEntry> records)
    {
        if (_chains is null)
        {
            return;
        }

        var defects = FRChainIntegrity.Check(records.Select(e => ((ArchiveEntry?) e, (ArchiveEntryException?) null)), null);
        foreach (var (code, label, information) in FRChainIntegrity.PerCode(defects))
        {
            await _chains.RaisePendingJetEventAsync(await _actionJournalRepository, code, label, information).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The French compliance parameters in force (archive-v2 §12, Ref LNE 1.8 Exigence 19): the middleware version,
    /// the queue's identification and the SCU's configuration as the SCU reports it. Keys that look like secrets are
    /// never written, whatever an SCU reports.
    /// </summary>
    private async Task<IReadOnlyList<ComplianceParameter>> ComplianceParametersAsync(ftQueueFR? queueFR)
    {
        var parameters = new List<ComplianceParameter>
        {
            new("Middleware", "SoftwareVersion", ArchiveV2Files.SoftwareVersion(typeof(JournalProcessor).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion)),
            new("Queue", "ftQueueID", _queueId.ToString("D")),
            new("Queue", "CountryCode", "FR"),
            new("Queue", "ftCashBoxIdentification", queueFR?.CashBoxIdentification),
            new("Queue", "Siret", queueFR?.Siret),
            new("Queue", "Sandbox", _sandbox ? "true" : "false"),
            new("Queue", "ValidationLevel", _validationLevel),
        };

        var info = _scuInfo is null ? null : await _scuInfo().ConfigureAwait(false);
        if (info is not null)
        {
            // Contract §12: the package first, then the SCU's configuration keys; its runtime state is no parameter.
            parameters.Add(new("SCU", "Package", FRSSCDInfoHelper.GetCertificationBody(info)));
            if (!string.IsNullOrEmpty(info.InfoData))
            {
                try
                {
                    using var document = JsonDocument.Parse(info.InfoData!);
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in document.RootElement.EnumerateObject().Where(p => !LooksSecret(p.Name) && !NotAParameter.Contains(p.Name)))
                        {
                            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                            {
                                continue;
                            }
                            parameters.Add(new("SCU", property.Name, property.Value.ValueKind switch
                            {
                                JsonValueKind.String => property.Value.GetString(),
                                JsonValueKind.Null => null,
                                _ => property.Value.GetRawText(),
                            }));
                        }
                    }
                }
                catch (JsonException)
                {
                    // An SCU that reports no readable info contributes its description and version only.
                }
            }
        }
        return parameters;
    }

    /// <summary>What an SCU reports that is not configuration: the package, given first, and whether its key loaded.</summary>
    private static readonly HashSet<string> NotAParameter = new(StringComparer.OrdinalIgnoreCase) { "CertificationBody", "SignatureCreationDataAvailable" };

    private static bool LooksSecret(string name)
        => new[] { "key", "secret", "password", "token", "connection" }.Any(word => name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

    /// <summary>
    /// How far around the period the storage is read. An item's TimeStamp is written with its response, after its
    /// receipt moment; the margin covers a clock set back in between, and an entry kept between two of the period
    /// whose moment lies past its end (contract section 10.3).
    /// </summary>
    private static readonly TimeSpan ReadMargin = TimeSpan.FromDays(1);

    /// <summary>
    /// The signed v2 entries stored around <paramref name="period"/>, in queue-row order: the candidates of
    /// <see cref="ArchiveSplitter.Select{TRecord}"/>, which keeps those from the first in the period to the last.
    /// Each entry carries its chain links as signed (ADR 0001), so the storage is read around the period only,
    /// never replayed from the queue's first item (ADR 0012). The entries' requests and responses stay in a
    /// spool file, not in memory. An item of the period that cannot be read fails the export.
    /// </summary>
    private async Task<SpooledEntries> ReadEntriesAsync(ArchivePeriod period, bool rejectV1)
    {
        var repository = await _queueItemRepository;
        var spool = new ArchiveEntrySpool();
        try
        {
            var entries = new List<ArchiveEntry>();
            var from = Math.Max(0, period.Start.Ticks - ReadMargin.Ticks);
            var to = period.End.Ticks > DateTime.MaxValue.Ticks - ReadMargin.Ticks ? DateTime.MaxValue.Ticks : period.End.Ticks + ReadMargin.Ticks;
            await foreach (var queueItem in repository.GetByTimeStampRangeAsync(from, to).ConfigureAwait(false))
            {
                ArchiveEntry? entry;
                try
                {
                    entry = ArchiveEntry.Read(queueItem.ftQueueItemId, queueItem.ftQueueId, queueItem.ftQueueRow, queueItem.request, queueItem.response, queueItem.ProcessingVersion);
                }
                catch (ArchiveEntryException) when (!period.Contains(MomentOf(queueItem)))
                {
                    continue;
                }
                if (entry is null)
                {
                    if (rejectV1 && period.Contains(MomentOf(queueItem)) && IsV1Entry(queueItem))
                    {
                        throw new NotSupportedException(
                            $"The period {period} holds entries of the v1 QueueFR (queue row {queueItem.ftQueueRow}). They are archived by v1.2 as v1.2 produces its archive (ADR 0003, ADR 0019): request the period from the first v2 entry on.");
                    }
                    continue;
                }
                entries.Add(spool.Keep(entry, queueItem.response));
            }

            // The storage returns items by TimeStamp; the archive lists them in queue-row order.
            entries.Sort((a, b) => a.QueueRow.CompareTo(b.QueueRow));
            return new SpooledEntries(spool, entries);
        }
        catch
        {
            spool.Dispose();
            throw;
        }
    }

    /// <summary>Where a stored item falls in time: the receipt moment of its response, v1 and v2 alike, else its queue moment.</summary>
    private static DateTime MomentOf(ftQueueItem queueItem)
        => ArchiveEntry.ReceiptMomentOf(queueItem.response) ?? DateTime.SpecifyKind(queueItem.ftQueueMoment, DateTimeKind.Utc);

    /// <summary>The entries of an export, and the spool that holds their content until the export is written.</summary>
    private sealed class SpooledEntries : IDisposable
    {
        private readonly ArchiveEntrySpool _spool;

        public SpooledEntries(ArchiveEntrySpool spool, List<ArchiveEntry> entries)
        {
            _spool = spool;
            Entries = entries;
        }

        public List<ArchiveEntry> Entries { get; }

        public void Dispose() => _spool.Dispose();
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

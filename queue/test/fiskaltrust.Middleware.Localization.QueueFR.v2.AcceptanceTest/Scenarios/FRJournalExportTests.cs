using System.IO.Compression;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.InMemory;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// Signs receipts through a real QueueFR.v2 and reads them back through its journal endpoint, so the
/// French exports are checked against what the queue actually stored (fiskaltrust/market-fr#218).
/// </summary>
public class FRJournalExportTests : IDisposable
{
    private const string Siret = "12345678901234";

    private readonly Guid _queueId = Guid.NewGuid();
    private readonly Guid _cashBoxId = Guid.NewGuid();
    private readonly Func<string, Task<string>> _signProcessor;
    private readonly Func<string, Task<(System.Net.Mime.ContentType contentType, PipeReader reader)>> _journal;
    private readonly InMemorySCU _scu = new();
    private readonly InMemoryLocalizationStorageProvider _storageProvider;

    public FRJournalExportTests()
    {
        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue> { new() { ftQueueId = _queueId, ftCashBoxId = _cashBoxId, StartMoment = DateTime.UtcNow, CountryCode = "FR" } }) },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR> { new() { ftQueueFRId = _queueId, CashBoxIdentification = "cashbox-fr", Siret = Siret } }) },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration { Account = new AccountMasterData { AccountId = Guid.NewGuid(), AccountName = "fiskaltrust SARL", Country = "FR" } }) },
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(_queueId, configuration, loggerFactory);
        _storageProvider = storageProvider;
        var bootstrapper = new QueueFRBootstrapper(_queueId, loggerFactory, configuration, _scu, storageProvider);
        _signProcessor = bootstrapper.RegisterForSign();
        _journal = bootstrapper.RegisterForJournal();
    }

    public void Dispose() => _scu.Dispose();

    [Fact]
    public async Task TicketJournal_ChainsEachTicketOnThePreviousSignature()
    {
        var first = await SignAsync(CashSale());
        var second = await SignAsync(CashSale());

        var rows = Rows(await ReadAsync(_journal, JournalTypeFR.Ticket));

        rows.Should().HaveCount(2);
        rows[0]["ftReceiptIdentification"].Should().Be(first.ftReceiptIdentification);
        rows[0]["StartOfChain"].Should().Be("true");
        rows[0]["PreviousSignature"].Should().BeEmpty();
        rows[1]["ftReceiptIdentification"].Should().Be(second.ftReceiptIdentification);
        rows[1]["StartOfChain"].Should().Be("false");
        rows[1]["PreviousSignature"].Should().Be(ChainedSignature(first));
        rows[1]["Siret"].Should().Be(second.ftSignatures.First(x => x.ftSignatureType.IsType((SignatureTypeFR) 0x4652_2000_0000_0009)).Data, "the SIRET the SCU signed");
        rows[1]["ftCashBoxIdentification"].Should().Be("cashbox-fr");
        rows[1]["RequestHash"].Should().BeEmpty("the in-memory SCU signs no canonical request value (ADR 0013), and the export never computes one");
    }

    [Fact]
    public async Task TicketJournal_OfALaterPeriod_StillChainsItsFirstEntryOnTheEntryBefore()
    {
        var first = await SignAsync(CashSale());
        var from = DateTime.UtcNow;
        var second = await SignAsync(CashSale());

        var rows = Rows(await ReadAsync(_journal, JournalTypeFR.Ticket, from.Ticks));

        rows.Should().ContainSingle().Which["ftReceiptIdentification"].Should().Be(second.ftReceiptIdentification);
        rows[0]["PreviousSignature"].Should().Be(ChainedSignature(first));
    }

    [Fact]
    public async Task DailyClosingJournal_HoldsTheDailyClosings_AndTheTechnicalJournalNone()
    {
        await SignAsync(CashSale());
        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        var rows = Rows(await ReadAsync(_journal, JournalTypeFR.DailyClosing));
        var technical = Rows(await ReadAsync(_journal, JournalTypeFR.Technical));

        rows.Should().ContainSingle().Which["ftReceiptIdentification"].Should().Be(closing.ftReceiptIdentification);
        technical.Should().NotContain(row => row["ftReceiptIdentification"] == closing.ftReceiptIdentification);
    }

    [Fact]
    public async Task Status_ReportsTheQueueAndTheJournalTypes()
    {
        var status = Encoding.UTF8.GetString(await ReadAsync(_journal, JournalTypeFR.StatusInformation));

        status.Should().Contain(Siret).And.Contain("\"Code\":\"00A\"");
    }

    /// <summary>
    /// The archive receipts are chain A, journal 008 (market-fr#221, 2026-11-09), so the archive
    /// export keeps them in a journal of their own instead of dropping them.
    /// </summary>
    [Fact]
    public async Task ArchiveJournal_HoldsTheArchiveReceipts_ChainedOnThePreviousOne()
    {
        var from = DateTime.UtcNow.AddMinutes(-1);
        await SignAsync(CashSale());
        var first = await SignAsync(Request((ReceiptCase) 0x3011));
        var second = await SignAsync(Request((ReceiptCase) 0x3011));

        var rows = Rows(await ReadAsync(_journal, JournalTypeFR.Archive));

        rows.Should().HaveCount(2);
        rows[0]["ftReceiptIdentification"].Should().Be(first.ftReceiptIdentification);
        rows[1]["ftReceiptIdentification"].Should().Be(second.ftReceiptIdentification);
        rows[1]["PreviousSignature"].Should().Be(ChainedSignature(first));

        var bytes = await ReadAsync(_journal, JournalTypeFR.ArchiveExport, from.Ticks);
        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var subArchive = new ZipArchive(zip.Entries.Single(e => e.Name.StartsWith("subarchive_")).Open());
        subArchive.Entries.Should().Contain(e => e.Name == "journal-A-archive.csv");
    }

    /// <summary>
    /// An archive ends on its own archive receipt (decided 2026-11-10), and chain A gets a chain anchor
    /// like every other chain: its first A receipt and the previous A receipt's signature, which the
    /// previous archive holds (contract section 10, market-fr#221).
    /// </summary>
    [Fact]
    public async Task ArchiveExport_AnchorsChainAOnThePreviousArchiveReceipt()
    {
        await SignAsync(CashSale());
        var first = await SignAsync(Request((ReceiptCase) 0x3011));
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());
        var second = await SignAsync(Request((ReceiptCase) 0x3011));

        var bytes = await ReadAsync(_journal, JournalTypeFR.ArchiveExport, from.Ticks);

        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var subArchive = new ZipArchive(zip.Entries.Single(e => e.Name.StartsWith("subarchive_")).Open());
        using var manifest = JsonDocument.Parse(ReadEntry(subArchive, ArchivePackager<object>.ManifestFileName));
        var anchor = manifest.RootElement.GetProperty("chainAnchors").EnumerateArray().Single(a => a.GetProperty("chain").GetString() == "A");
        anchor.GetProperty("firstReceiptIdentification").GetString().Should().Be(second.ftReceiptIdentification);
        anchor.GetProperty("previousSignature").GetString().Should().Be(ChainedSignature(first));
    }

    [Fact]
    public async Task ArchiveReceipt_AsTheQueuesFirstReceipt_IsRefused()
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _signProcessor(JsonSerializer.Serialize(Request((ReceiptCase) 0x3011))))!;

        response.ftState.IsState(State.Error).Should().BeTrue("as v1, the first receipt of a queue has nothing to archive");
        response.ftReceiptIdentification.Should().NotContain("A1");
    }

    /// <summary>
    /// As v1.2, an archive receipt signs which receipts its archive holds, the last journals, the day,
    /// month and year totals and the previous archive receipt; the archive's bytes are not hashed
    /// (market-fr#221). Unlike v1.2, the range runs from the record after the previous archive receipt
    /// up to and including the archive receipt itself, so every record lies in exactly one range
    /// (decided 2026-11-10).
    /// </summary>
    [Fact]
    public async Task ArchiveReceipt_SignsTheBoundariesOfTheArchiveItCloses()
    {
        var firstSale = await SignAsync(CashSale());
        var first = await SignAsync(Request((ReceiptCase) 0x3011));
        var secondSale = await SignAsync(CashSale());
        var second = await SignAsync(Request((ReceiptCase) 0x3011));

        var firstBoundaries = SCU.FR.Abstraction.Helpers.FRStateData.Archive(first)!;
        firstBoundaries.PreviousArchiveQueueItemId.Should().BeNull("it is the queue's first archive receipt");
        firstBoundaries.FirstContainedReceiptQueueItemId.Should().Be(firstSale.ftQueueItemID);
        firstBoundaries.FirstContainedReceiptMoment.Should().Be(firstSale.ftReceiptMoment);
        firstBoundaries.LastContainedReceiptQueueItemId.Should().Be(first.ftQueueItemID, "the archive receipt is its own last contained receipt");
        firstBoundaries.LastContainedReceiptMoment.Should().Be(first.ftReceiptMoment);
        firstBoundaries.LastReceiptJournalId.Should().NotBeNull();
        firstBoundaries.Day.Totalizer.Should().Be(12.00m);

        var secondBoundaries = SCU.FR.Abstraction.Helpers.FRStateData.Archive(second)!;
        secondBoundaries.PreviousArchiveQueueItemId.Should().Be(first.ftQueueItemID);
        secondBoundaries.FirstContainedReceiptQueueItemId.Should().Be(secondSale.ftQueueItemID, "the range starts on the record after the previous archive receipt");
        secondBoundaries.FirstContainedReceiptMoment.Should().Be(secondSale.ftReceiptMoment);
        secondBoundaries.LastContainedReceiptQueueItemId.Should().Be(second.ftQueueItemID);
        secondBoundaries.LastContainedReceiptMoment.Should().Be(second.ftReceiptMoment);
        secondBoundaries.Day.Totalizer.Should().Be(12.00m, "the first archive receipt closed the day");
        secondBoundaries.Year.Totalizer.Should().Be(24.00m);

        var signedContent = SCU.FR.Abstraction.Helpers.FRReceiptQrCode.SignedContentOf(second.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);
        signedContent.Should().Contain(first.ftQueueItemID.ToString("D"), "the SCU signs the link to the previous archive receipt");
    }

    [Fact]
    public async Task TrainingArchiveReceipt_SignsNoBoundaries()
    {
        await SignAsync(CashSale());
        var request = Request((ReceiptCase) 0x3011);
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Training);

        var training = await SignAsync(request);

        SCU.FR.Abstraction.Helpers.FRStateData.Archive(training).Should().BeNull();
        training.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypeFR.DayTotals), "a training archive receipt prints the live day totals, as v1.2");
    }

    [Fact]
    public async Task ArchiveExport_IsSignedByTheQueuesScuAndVerifies()
    {
        var from = DateTime.UtcNow.AddMinutes(-1);
        var sale = await SignAsync(CashSale());

        var bytes = await ReadAsync(_journal, JournalTypeFR.ArchiveExport, from.Ticks);

        ArchiveVerifier.Verify(new MemoryStream(bytes)).Errors.Should().BeEmpty();
        using var zip = new ZipArchive(new MemoryStream(bytes));
        var subArchive = zip.Entries.Should().ContainSingle(e => e.Name.StartsWith("subarchive_")).Subject;
        using var sub = new ZipArchive(subArchive.Open());
        var tickets = ReadCsv(sub, "journal-T-ticket.csv");
        tickets.Should().ContainSingle(r => r["ftReceiptIdentification"] == sale.ftReceiptIdentification);
        var ticket = tickets.Single(r => r["ftReceiptIdentification"] == sale.ftReceiptIdentification);
        ticket["Totalizer"].Should().NotBeEmpty("the queue stores the receipt totals when it signs (market-fr#221)");
        ticket["SoftwareVersion"].Should().NotBeEmpty("the queue item records the version that processed it");
        ReadCsv(sub, "charge-items.csv").Should().OnlyContain(r => r["AmountHT"] != "" && r["VATAmount"] != "" && r["PricesIncludeVAT"] == "true");
        ReadCsv(sub, "compliance-parameters.csv").Should().Contain(r => r["Component"] == "Queue" && r["Parameter"] == "ftQueueID");
        ReadCsv(sub, "compliance-parameters.csv").Should().ContainSingle(r => r["Component"] == "Queue" && r["Parameter"] == "ValidationLevel", "it is written even when not configured (market-fr#221)");
        sub.Entries.Select(e => e.Name).Should().Contain(new[] { "charge-items.csv", "pay-items.csv", "receipt-vat-summary.csv", "signatures.csv", "jet.csv", "case-data.csv", "compliance-parameters.csv", "README.txt" })
            .And.NotContain("journal-types.csv");

        using var archiveCertificate = new X509Certificate2(ReadEntry(zip, ArchivePackager<object>.ArchiveCertificateFileName));
        using var scuKey = new X509Certificate2(Convert.FromBase64String((await _scu.GetSigningCertificateAsync()).CertificateBase64)).GetECDsaPublicKey()!;
        archiveCertificate.GetECDsaPublicKey()!.ExportSubjectPublicKeyInfo().Should().Equal(scuKey.ExportSubjectPublicKeyInfo(), "the queue's SCU signs a locally exported archive (ADR 0007 amendment)");
        sub.Entries.Should().Contain(e => e.Name == "scu_INMEMORY_0001_v2_1.cer", "the SCU certificate also checks the entries it signed");
    }

    [Fact]
    public async Task ArchiveExport_CarriesTheStoredRequestVerbatim()
    {
        var from = DateTime.UtcNow.AddMinutes(-1);
        var sale = await SignAsync(CashSale());

        var bytes = await ReadAsync(_journal, JournalTypeFR.ArchiveExport, from.Ticks);

        var repository = await _storageProvider.CreateMiddlewareQueueItemRepository();
        var stored = (await repository.GetAsync()).Single(q => q.ftQueueItemId == sale.ftQueueItemID);
        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var sub = new ZipArchive(zip.Entries.Single(e => e.Name.StartsWith("subarchive_")).Open());
        var lines = Encoding.UTF8.GetString(ReadEntry(sub, "requests.jsonl")).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        using var line = JsonDocument.Parse(lines.Single());
        line.RootElement.GetProperty("ftQueueItemID").GetString().Should().Be(sale.ftQueueItemID.ToString("D"));
        line.RootElement.GetProperty("request").GetString().Should().Be(stored.request, "a verifier recomputes the request hash from the stored bytes (archive-v2 section 9)");
    }

    [Fact]
    public async Task ArchiveExport_OverTwoFiscalYears_IsRefused()
    {
        var act = () => ReadAsync(_journal, JournalTypeFR.ArchiveExport, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc).Ticks, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc).Ticks);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*fiscal year*");
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _signProcessor(JsonSerializer.Serialize(request)))!;
        response.ftState.IsState(State.Error).Should().BeFalse();
        return response;
    }

    private static async Task<byte[]> ReadAsync(Func<string, Task<(System.Net.Mime.ContentType, PipeReader)>> journal, JournalTypeFR type, long from = 0, long to = 0)
    {
        var (_, reader) = await journal(JsonSerializer.Serialize(new JournalRequest { ftJournalType = type.As<JournalType>(), From = from, To = to }));
        using var buffer = new MemoryStream();
        await reader.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private static byte[] ReadEntry(ZipArchive zip, string name)
    {
        using var buffer = new MemoryStream();
        using (var stream = zip.GetEntry(name)!.Open())
        {
            stream.CopyTo(buffer);
        }
        return buffer.ToArray();
    }

    private static List<Dictionary<string, string>> Rows(byte[] csv)
    {
        // The test data holds no ';' or quote, so a plain split reads the file.
        var lines = Encoding.UTF8.GetString(csv).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].Split(';');
        return lines.Skip(1).Select(l => header.Zip(l.Split(';')).ToDictionary(p => p.First, p => p.Second)).ToList();
    }

    private static string ChainedSignature(ReceiptResponse response)
    {
        var signature = response.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        return signature[(signature.LastIndexOf('.') + 1)..];
    }

    private ReceiptRequest Request(ReceiptCase receiptCase) => new()
    {
        ftCashBoxID = _cashBoxId,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
        cbReceiptMoment = DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = receiptCase.WithCountry("FR"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private ReceiptRequest CashSale()
    {
        var request = Request(ReceiptCase.PointOfSaleReceipt0x0001);
        request.cbChargeItems =
        [
            new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
        ];
        request.cbPayItems =
        [
            new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") },
        ];
        return request;
    }

    private static List<Dictionary<string, string>> ReadCsv(ZipArchive zip, string name)
    {
        using var reader = new StreamReader(zip.GetEntry(name)!.Open());
        var lines = reader.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].Split(';');
        // The rows checked here hold no quoted separator.
        return lines.Skip(1).Select(l => l.Split(';')).Select(cells => header.Select((h, i) => (h, v: i < cells.Length ? cells[i] : "")).ToDictionary(x => x.h, x => x.v)).ToList();
    }
}

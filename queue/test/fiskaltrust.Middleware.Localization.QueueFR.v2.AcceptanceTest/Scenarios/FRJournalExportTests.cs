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
    public async Task GrandTotalJournal_HoldsTheClosingChains()
    {
        await SignAsync(CashSale());
        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        var rows = Rows(await ReadAsync(_journal, JournalTypeFR.GrandTotal));

        rows.Should().ContainSingle().Which["ftReceiptIdentification"].Should().Be(closing.ftReceiptIdentification);
    }

    [Fact]
    public async Task Status_ReportsTheQueueAndTheJournalTypes()
    {
        var status = Encoding.UTF8.GetString(await ReadAsync(_journal, JournalTypeFR.StatusInformation));

        status.Should().Contain(Siret).And.Contain("\"Code\":\"00A\"");
    }

    [Fact]
    public async Task ArchiveJournal_IsRefusedUntilTheQueueSignsArchiveReceipts()
    {
        var act = () => ReadAsync(_journal, JournalTypeFR.Archive);

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*archive receipts*");
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
        using var reader = new StreamReader(sub.GetEntry("journal-T.csv")!.Open());
        (await reader.ReadToEndAsync()).Should().Contain(sale.ftReceiptIdentification);
        sub.Entries.Select(e => e.Name).Should().Contain(new[] { "charge-items.csv", "pay-items.csv", "journal-types.csv", "case-data.csv" });

        using var archiveCertificate = new X509Certificate2(ReadEntry(zip, ArchivePackager<object>.ArchiveCertificateFileName));
        using var scuKey = new X509Certificate2(Convert.FromBase64String((await _scu.GetSigningCertificateAsync()).CertificateBase64)).GetECDsaPublicKey()!;
        archiveCertificate.GetECDsaPublicKey()!.ExportSubjectPublicKeyInfo().Should().Equal(scuKey.ExportSubjectPublicKeyInfo(), "the queue's SCU signs a locally exported archive (ADR 0007 amendment)");
        sub.Entries.Should().Contain(e => e.Name == "scu_INMEMORY_0001_v2.cer", "the SCU certificate also checks the entries it signed");
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
}

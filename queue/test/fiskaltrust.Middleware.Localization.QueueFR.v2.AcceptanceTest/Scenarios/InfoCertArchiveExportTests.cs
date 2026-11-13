using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// An InfoCert queue archives a period that holds a technical event (JET): the archive attributes
/// every entry to its SCU by the certificate serial in the signed content (market-fr#262).
/// </summary>
public class InfoCertArchiveExportTests
{
    private readonly Guid _cashBoxId = Guid.NewGuid();
    private readonly Func<string, Task<string>> _signProcessor;
    private readonly Func<string, Task<(System.Net.Mime.ContentType contentType, PipeReader reader)>> _journal;

    public InfoCertArchiveExportTests()
    {
        var queueId = Guid.NewGuid();
        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue> { new() { ftQueueId = queueId, ftCashBoxId = _cashBoxId, StartMoment = DateTime.UtcNow, CountryCode = "FR" } }) },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR> { new() { ftQueueFRId = queueId, CashBoxIdentification = "cashbox-fr", Siret = "12345678901234" } }) },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration { Account = new AccountMasterData { AccountId = Guid.NewGuid(), AccountName = "fiskaltrust SARL", Country = "FR" } }) },
        };

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = new CertificateRequest("CN=SCU FR", key, HashAlgorithmName.SHA256).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        var scu = new InfoCertFRSSCD(new InfoCertConfiguration
        {
            Siret = "12345678901234",
            PrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
            CertificateBase64 = Convert.ToBase64String(certificate.RawData),
            CertificateSerialNumber = "CERT-4711",
            AttestationNumber = "IC-2026-001",
            SoftwareVersion = "1.0.0",
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(queueId, configuration, loggerFactory);
        var bootstrapper = new QueueFRBootstrapper(queueId, loggerFactory, configuration, scu, storageProvider);
        _signProcessor = bootstrapper.RegisterForSign();
        _journal = bootstrapper.RegisterForJournal();
    }

    [Fact]
    public async Task ArchiveExport_OfAPeriodWithATechnicalEvent_Verifies()
    {
        var from = DateTime.UtcNow.AddMinutes(-1);
        await SignAsync(CashSale());
        var jet = Request(ReceiptCase.ProtocolTechnicalEvent0x3001);
        jet.ftReceiptCaseData = JsonDocument.Parse("{\"FR\":{\"Code\":\"90\",\"Message\":\"Archivage\"}}").RootElement.Clone();
        (await SignAsync(jet)).ftReceiptIdentification.Should().MatchRegex("L[0-9]+$");

        var (_, reader) = await _journal(JsonSerializer.Serialize(new JournalRequest { ftJournalType = JournalTypeFR.ArchiveExport.As<JournalType>(), From = from.Ticks }));
        using var buffer = new MemoryStream();
        await reader.CopyToAsync(buffer);

        ArchiveVerifier.Verify(new MemoryStream(buffer.ToArray())).Errors.Should().BeEmpty();
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _signProcessor(JsonSerializer.Serialize(request)))!;
        response.ftState.IsState(State.Error).Should().BeFalse(string.Join(" / ", response.ftSignatures.Select(x => x.Data)));
        return response;
    }

    private ReceiptRequest Request(ReceiptCase receiptCase) => new()
    {
        ftCashBoxID = _cashBoxId,
        cbTerminalID = "1",
        cbUser = "operator-1",
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
        request.cbChargeItems = [new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }];
        request.cbPayItems = [new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];
        return request;
    }
}

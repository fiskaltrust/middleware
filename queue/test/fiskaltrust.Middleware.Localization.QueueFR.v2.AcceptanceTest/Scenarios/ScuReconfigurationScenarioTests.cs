using System.Security.Cryptography;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// Changing the SCU of a French queue is configuration only (market-fr#177): connect the other
/// SCU, rebuild the configuration, restart. There is no switch receipt; the queue reads its chain
/// state back from ftQueueFR (market-fr ADR 0012), so every chain continues under the new SCU.
/// </summary>
public class ScuReconfigurationScenarioTests
{
    private const string Siret = "12345678901234";

    private readonly Guid _queueId = Guid.NewGuid();
    private readonly Guid _cashBoxId = Guid.NewGuid();
    private readonly Dictionary<string, object> _configuration;
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private readonly InMemoryLocalizationStorageProvider _storageProvider;

    public ScuReconfigurationScenarioTests()
    {
        _configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue> { new() { ftQueueId = _queueId, ftCashBoxId = _cashBoxId, StartMoment = DateTime.UtcNow, CountryCode = "FR" } }) },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR> { new() { ftQueueFRId = _queueId, CashBoxIdentification = "cashbox-fr", Siret = Siret } }) },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration { Account = new AccountMasterData { AccountId = Guid.NewGuid(), AccountName = "fiskaltrust SARL", Country = "FR" } }) },
        };
        _storageProvider = new InMemoryLocalizationStorageProvider(_queueId, _configuration, _loggerFactory);
    }

    /// <summary>Starts the queue against the shared storage, as the restart after a reconfiguration does.</summary>
    private Func<string, Task<string>> Start(IFRSSCD scu)
        => new QueueFRBootstrapper(_queueId, _loggerFactory, _configuration, scu, _storageProvider).RegisterForSign();

    private static string NewKey()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return Convert.ToBase64String(key.ExportPkcs8PrivateKey());
    }

    [Fact]
    public async Task AReconfiguredScu_ContinuesEveryChainFromFtQueueFR()
    {
        var lne = Start(new LneFRSSCD(new LneConfiguration { Siret = Siret, PrivateKey = NewKey(), CertificateSerialNumber = "LNE-CERT" }));
        var firstSale = await SignAsync(lne, CashSale());
        var firstClosing = await SignAsync(lne, Request(ReceiptCase.DailyClosing0x2011));

        var selfCertified = Start(new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration { Siret = Siret, PrivateKey = NewKey(), CertificateSerialNumber = "SELF-CERT" }));
        var secondSale = await SignAsync(selfCertified, CashSale());
        var secondClosing = await SignAsync(selfCertified, Request(ReceiptCase.DailyClosing0x2011));

        foreach (var response in new[] { firstSale, firstClosing, secondSale, secondClosing })
        {
            response.ftState.IsState(State.Error).Should().BeFalse();
        }

        secondSale.ftReceiptIdentification.Should().EndWith("T2", "the ticket counter carries on under the new SCU");
        ChainingFieldsOf(secondSale).Should().Equal("2", "O", SignatureOf(firstSale, SignatureTypeFR.ReceiptSignature));
        secondClosing.ftReceiptIdentification.Should().EndWith("Z2");
        ChainingFieldsOf(secondClosing).Should().Equal("2", "O", SignatureOf(firstClosing, SignatureTypeFR.ReceiptSignature));
        CertificateOf(secondSale).Should().Be("SELF-CERT");

        var perpetual = SignedDataOf(secondClosing).Split('|')[^5].Split(',')[12];
        perpetual.Should().Be("24.00", "the perpetual total carries both SCUs' sales");
    }

    private static async Task<ReceiptResponse> SignAsync(Func<string, Task<string>> signProcessor, ReceiptRequest request)
        => JsonSerializer.Deserialize<ReceiptResponse>(await signProcessor(JsonSerializer.Serialize(request)))!;

    /// <summary>The certificate serial item (0x4652_2000_0000_0002); the queue-local enum has no member for it until the interface is published.</summary>
    private static string CertificateOf(ReceiptResponse response)
        => response.ftSignatures.First(x => ((long) x.ftSignatureType & 0xFFFF) == 0x0002).Data;

    private static string? SignatureOf(ReceiptResponse response, SignatureTypeFR type)
        => response.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(type))?.Data;

    /// <summary>The data set LNE and the self-certified SCU sign, both ending with counter, flag and previous signature (ADR 0001).</summary>
    private static string SignedDataOf(ReceiptResponse response)
        => response.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information) && x.Caption == "Donnees signees").Data;

    private static string[] ChainingFieldsOf(ReceiptResponse response) => SignedDataOf(response).Split('|')[^3..];

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
        request.cbChargeItems = [new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }];
        request.cbPayItems = [new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];
        return request;
    }
}

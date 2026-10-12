using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// Drives a real QueueFR.v2 through its sign processor against the in-memory storage and an SCU,
/// so the chaining, numbering and signature wiring is exercised end to end. Each SCU the queue
/// supports runs the same scenarios through a subclass.
/// </summary>
public abstract class FRScenarioTests
{
    private const string CashBoxIdentification = "cashbox-fr";

    /// <summary>The SCUs' certificate serial item; the queue's copy of SignatureTypeFR does not name it.</summary>
    private const SignatureTypeFR CertificateSerialNumber = (SignatureTypeFR) 0x4652_2000_0000_0002;

    private readonly Guid _queueId;
    private readonly Guid _cashBoxId;
    private readonly Dictionary<string, object> _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly InMemoryLocalizationStorageProvider _storageProvider;
    private Func<string, Task<string>> _signProcessor;

    protected abstract IFRSSCD CreateScu();

    /// <summary>The SCU an SCU switch moves to: another certificate, possibly another certification body.</summary>
    protected abstract IFRSSCD CreateIncomingScu();

    protected FRScenarioTests()
    {
        var queueId = Guid.NewGuid();
        _queueId = queueId;
        _cashBoxId = Guid.NewGuid();

        var configuration = _configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue>
                {
                    new() { ftQueueId = queueId, ftCashBoxId = _cashBoxId, StartMoment = DateTime.UtcNow, CountryCode = "FR" },
                })
            },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR>
                {
                    new() { ftQueueFRId = queueId, CashBoxIdentification = CashBoxIdentification, Siret = "12345678901234" },
                })
            },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration
                {
                    Account = new AccountMasterData
                    {
                        AccountId = Guid.NewGuid(),
                        AccountName = "fiskaltrust SARL",
                        Street = "1 rue de la Paix",
                        Zip = "75002",
                        City = "Paris",
                        Country = "FR",
                    },
                })
            },
        };

        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _storageProvider = new InMemoryLocalizationStorageProvider(queueId, configuration, _loggerFactory);
        _signProcessor = Start(CreateScu());
    }

    /// <summary>
    /// Starts the queue against the shared storage, as a restart with a reconfigured SCU does: the
    /// fiscal state is rebuilt from the stored queue items.
    /// </summary>
    private Func<string, Task<string>> Start(IFRSSCD scu)
        => new QueueFRBootstrapper(_queueId, _loggerFactory, _configuration, scu, _storageProvider).RegisterForSign();

    [Fact]
    public async Task ScuSwitch_ContinuesTheChainUnderTheIncomingScu()
    {
        await SignAsync(CashSale());
        var init = await SignAsync(Request(ReceiptCase.InitSCUSwitch0x4011));
        var refusedSale = await SignAsync(CashSale());

        _signProcessor = Start(CreateIncomingScu());
        var finish = await SignAsync(Request(ReceiptCase.FinishSCUSwitch0x4012));
        var sale = await SignAsync(CashSale());

        init.ftState.IsState(State.Error).Should().BeFalse();
        init.ftReceiptIdentification.Should().EndWith("G1");
        refusedSale.ftState.IsState(State.Error).Should().BeTrue("nothing may be signed between 0x4011 and 0x4012");
        finish.ftState.IsState(State.Error).Should().BeFalse();
        finish.ftReceiptIdentification.Should().EndWith("G2", "the switch neither closes nor restarts a chain (ADR 0005)");
        SignatureOf(finish, CertificateSerialNumber).Should().NotBe(SignatureOf(init, CertificateSerialNumber));
        sale.ftState.IsState(State.Error).Should().BeFalse();
        sale.ftReceiptIdentification.Should().EndWith("T2");
        SignatureOf(sale, CertificateSerialNumber).Should().Be(SignatureOf(finish, CertificateSerialNumber));

        var scuSwitch = JsonSerializer.Deserialize<MiddlewareStateData>(JsonSerializer.Serialize(finish.ftStateData))!.FR!.ScuSwitch!;
        scuSwitch.Incoming!.CertificateSerialNumber.Should().Be(SignatureOf(finish, CertificateSerialNumber));
        scuSwitch.Outgoing.CertificateSerialNumber.Should().Be(SignatureOf(init, CertificateSerialNumber));
        PreviousSignatureCoveredBy(finish)?.Should().Be(SignatureOf(init, SignatureTypeFR.ReceiptSignature), "the incoming SCU's first entry covers the outgoing SCU's last signature");
    }

    [Fact]
    public async Task FinishScuSwitch_WithoutInit_IsRejected()
    {
        var response = await SignAsync(Request(ReceiptCase.FinishSCUSwitch0x4012));

        response.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task FinishScuSwitch_WithoutReconfiguringTheScu_IsRejectedAndTheSwitchStaysPending()
    {
        await SignAsync(Request(ReceiptCase.InitSCUSwitch0x4011));

        var finish = await SignAsync(Request(ReceiptCase.FinishSCUSwitch0x4012));
        var sale = await SignAsync(CashSale());

        finish.ftState.IsState(State.Error).Should().BeTrue("the switch has to change the certificate the archive is split by");
        sale.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task CashSale_IsSignedAndNumberedInTheTicketChain()
    {
        var response = await SignAsync(CashSale());

        response.ftState.IsState(State.Error).Should().BeFalse();
        response.ftReceiptIdentification.Should().EndWith("T1");
        SignatureOf(response, SignatureTypeFR.ReceiptSignature).Should().NotBeNullOrEmpty();
        SignatureOf(response, SignatureTypeFR.ReceiptSignature).Should().MatchRegex("^[A-Za-z0-9_-]+$", "chained signatures are stored in Base64Url (ADR 0001)");
        SignatureOf(response, SignatureTypeFR.ChainHash).Should().BeNull("an entry chains on the previous signature, not on a separate hash");
    }

    [Fact]
    public async Task ConsecutiveSales_ContinueTheSameChain()
    {
        var first = await SignAsync(CashSale());
        var second = await SignAsync(CashSale());

        first.ftReceiptIdentification.Should().EndWith("T1");
        second.ftReceiptIdentification.Should().EndWith("T2");
        SignatureOf(second, SignatureTypeFR.ReceiptSignature).Should().NotBe(SignatureOf(first, SignatureTypeFR.ReceiptSignature));
    }

    [Fact]
    public async Task InvoiceAndTicket_UseSeparateChains()
    {
        await SignAsync(CashSale());
        var invoice = await SignAsync(Request(ReceiptCase.InvoiceB2B0x1002));

        invoice.ftState.IsState(State.Error).Should().BeFalse();
        invoice.ftReceiptIdentification.Should().EndWith("I1");
    }

    [Fact]
    public async Task DailyClosing_IsSignedIntoTheGrandTotalChain()
    {
        var response = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        response.ftState.IsState(State.Error).Should().BeFalse();
        response.ftReceiptIdentification.Should().EndWith("G1");
    }

    [Fact]
    public async Task DailyClosing_ReportsTheTurnoverAccumulatedSinceTheLastClosing()
    {
        await SignAsync(CashSale());
        await SignAsync(CashSale());
        var closing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        // Two 12.00 EUR sales - a closing attests the period, not its own (empty) item list.
        TotalsOf(closing).Should().Be(("24.00", "24.00"));

        await SignAsync(CashSale());
        var secondClosing = await SignAsync(Request(ReceiptCase.DailyClosing0x2011));

        TotalsOf(secondClosing).Should().Be(("12.00", "36.00"), "the first closing reset the day, the perpetual total is never reset");
    }

    [Fact]
    public async Task NonEuroReceipt_IsRejected()
    {
        var request = CashSale();
        request.Currency = Currency.CHF;

        var response = await SignAsync(request);

        response.ftState.IsState(State.Error).Should().BeTrue("a French queue totalizes in EUR only");
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var responseJson = await _signProcessor(JsonSerializer.Serialize(request));
        var response = JsonSerializer.Deserialize<ReceiptResponse>(responseJson);
        response.Should().NotBeNull();
        return response!;
    }

    /// <summary>
    /// The day and perpetual totalizers a closing attests. By default read off the printed total
    /// items; an SCU that only carries them inside its signed data set overrides this.
    /// </summary>
    protected virtual (string? Day, string? Perpetual) TotalsOf(ReceiptResponse closing)
        => (SignatureOf(closing, SignatureTypeFR.DayTotals), SignatureOf(closing, SignatureTypeFR.PerpetualTotals));

    /// <summary>
    /// The previous signature the SCU covered with this entry, for an SCU that publishes its signed
    /// data set; null when it does not, and the check is skipped.
    /// </summary>
    protected virtual string? PreviousSignatureCoveredBy(ReceiptResponse response) => null;

    /// <summary>
    /// The day and perpetual totalizers of a closing, read off the signed data set of an SCU that
    /// prints no total items (LNE, self-certified): field 12 is <c>period,current totalizer,...,
    /// perpetual totalizer,...</c> with 11 amounts each.
    /// </summary>
    protected static (string? Day, string? Perpetual) TotalsFromSignedDataSet(ReceiptResponse closing)
    {
        var totals = SignatureOf(closing, SignatureTypeFR.Information)!.Split('|')[11].Split(',');
        totals[0].Should().Be("Day");
        return (totals[1], totals[12]);
    }

    /// <summary>The last field of the signed data set: the previous signature it covers (ADR 0001).</summary>
    protected static string? LastFieldOfSignedDataSet(ReceiptResponse response)
        => SignatureOf(response, SignatureTypeFR.Information)?.Split('|')[^1];

    /// <summary>Reads a signature off the response the way a POS would, by its FR signature type.</summary>
    protected static string? SignatureOf(ReceiptResponse response, SignatureTypeFR type)
        => response.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(type))?.Data;

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

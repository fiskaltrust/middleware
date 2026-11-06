using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class ProcessorTests
{
    private static (FRSigningPipeline pipeline, FakeFRSSCD sscd) CreatePipeline()
    {
        var sscd = new FakeFRSSCD();
        return (new FRSigningPipeline(sscd, new StoredChainState().Provider()), sscd);
    }

    private static ProcessCommandRequest CommandRequest(ReceiptCase receiptCase)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = receiptCase.WithCountry("FR"), Currency = Currency.EUR },
            new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            });

    /// <summary>
    /// v1.2 signs a sale without obligation as a foreign sale (v0 0x0E) in the bill chain; an
    /// e-commerce sale is a ticket (market-fr#221). Neither carries a mention.
    /// </summary>
    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003, "ft1#B1")]
    [InlineData(ReceiptCase.ECommerce0x0004, "ft1#T1")]
    public async Task SalesWithoutObligationAndECommerce_AreSignedIntoTheirChain(ReceiptCase receiptCase, string identification)
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);
        var request = CommandRequest(receiptCase);

        var response = receiptCase == ReceiptCase.ECommerce0x0004
            ? await sut.ECommerce0x0004Async(request)
            : await sut.PointOfSaleReceiptWithoutObligation0x0003Async(request);

        response.receiptResponse.ftReceiptIdentification.Should().Be(identification);
        response.receiptResponse.ftSignatures.Should().NotContain(x => x.Caption == "Document provisoire");
        sscd.Calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData((ReceiptCase) 0x0007)]
    [InlineData(ReceiptCase.Order0x3004)]
    public async Task ProvisionalDocuments_AreMarkedAndSignedIntoTheBillChain(ReceiptCase receiptCase)
    {
        var (pipeline, sscd) = CreatePipeline();
        var request = CommandRequest(receiptCase);

        var response = receiptCase switch
        {
            (ReceiptCase) 0x0006 => await new ReceiptCommandProcessorFR(pipeline).TableCheck0x0006Async(request),
            (ReceiptCase) 0x0007 => await new ReceiptCommandProcessorFR(pipeline).ProForma0x0007Async(request),
            _ => await ProtocolProcessor(pipeline).Order0x3004Async(request),
        };

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#B1");
        response.receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Document provisoire");
        sscd.Calls.Should().ContainSingle();
    }

    /// <summary>v1.2 neither signs nor numbers a delivery note (v0 0x09), an unspecified protocol (v0 0x14) or an internal consumption (v0 0x0D).</summary>
    [Theory]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    public async Task UnsignedDocuments_AreStoredButNeitherSignedNorNumbered(ReceiptCase receiptCase)
    {
        var (pipeline, sscd) = CreatePipeline();
        var request = CommandRequest(receiptCase);

        var response = receiptCase switch
        {
            ReceiptCase.DeliveryNote0x0005 => await new ReceiptCommandProcessorFR(pipeline).DeliveryNote0x0005Async(request),
            ReceiptCase.ProtocolUnspecified0x3000 => await ProtocolProcessor(pipeline).ProtocolUnspecified0x3000Async(request),
            _ => await ProtocolProcessor(pipeline).InternalUsageMaterialConsumption0x3003Async(request),
        };

        sscd.Calls.Should().BeEmpty();
        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
        response.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
    }

    /// <summary>v1.2 prints "justificatif non valable pour encaissement" on every payment proof (v0 0x02).</summary>
    [Theory]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    [InlineData(ReceiptCase.Pay0x3005)]
    public async Task PaymentProofs_CarryTheMentionAndAreSignedIntoThePaymentProofChain(ReceiptCase receiptCase)
    {
        var (pipeline, sscd) = CreatePipeline();
        var request = CommandRequest(receiptCase);

        var response = receiptCase == ReceiptCase.Pay0x3005
            ? await ProtocolProcessor(pipeline).Pay0x3005Async(request)
            : await new ReceiptCommandProcessorFR(pipeline).PaymentTransfer0x0002Async(request);

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#P1");
        response.receiptResponse.ftSignatures.Should().Contain(x => x.Data == "Justificatif non valable pour encaissement");
        sscd.Calls.Should().ContainSingle();
    }

    private static ProtocolCommandProcessorFR ProtocolProcessor(FRSigningPipeline pipeline)
        => new(pipeline, new AsyncLazy<IJournalFRCopyPayloadRepository>(() => Task.FromResult<IJournalFRCopyPayloadRepository>(new InMemoryJournalFRCopyPayloadRepository())), new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(new InMemoryQueueItemRepository())));

    private static ProcessCommandRequest CommandRequestWithLines(ReceiptCase receiptCase)
    {
        var request = CommandRequest(receiptCase);
        var sale = ChargeItemCase.NormalVatRate.WithCountry("FR");
        request.ReceiptRequest.cbChargeItems =
        [
            new ChargeItem { Amount = 10m, Currency = Currency.EUR, ftChargeItemCase = sale },
            new ChargeItem { Amount = 5m, Currency = Currency.EUR, ftChargeItemCase = sale },
            new ChargeItem { Amount = -1m, Currency = Currency.EUR, ftChargeItemCase = sale.WithFlag(ChargeItemCaseFlags.ExtraOrDiscount) },
        ];
        return request;
    }

    private static Task<ProcessCommandResponse> Process(ReceiptCommandProcessorFR sut, ReceiptCase receiptCase, ProcessCommandRequest request) => receiptCase switch
    {
        ReceiptCase.UnknownReceipt0x0000 => sut.UnknownReceipt0x0000Async(request),
        ReceiptCase.PointOfSaleReceipt0x0001 => sut.PointOfSaleReceipt0x0001Async(request),
        ReceiptCase.PaymentTransfer0x0002 => sut.PaymentTransfer0x0002Async(request),
        ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 => sut.PointOfSaleReceiptWithoutObligation0x0003Async(request),
        ReceiptCase.ECommerce0x0004 => sut.ECommerce0x0004Async(request),
        ReceiptCase.DeliveryNote0x0005 => sut.DeliveryNote0x0005Async(request),
        (ReceiptCase) 0x0006 => sut.TableCheck0x0006Async(request),
        (ReceiptCase) 0x0007 => sut.ProForma0x0007Async(request),
        _ => throw new ArgumentOutOfRangeException(nameof(receiptCase)),
    };

    [Theory]
    [InlineData(ReceiptCase.UnknownReceipt0x0000)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData((ReceiptCase) 0x0007)]
    public async Task TicketsAndNotes_CarryTheSalesLineCountAsASignatureItemSeenByTheSCU(ReceiptCase receiptCase)
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);

        var response = await Process(sut, receiptCase, CommandRequestWithLines(receiptCase));

        var item = response.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "Nombre de lignes").Which;
        item.Data.Should().Be("2", "the discount line is not a sales line");
        item.ftSignatureType.IsType(SignatureTypeFR.Information).Should().BeTrue();
        sscd.Calls.Should().ContainSingle();
        sscd.LastRequest!.ReceiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Nombre de lignes",
            "the item is added before signing, so the SCU sees it");
    }

    [Theory]
    [InlineData(ReceiptCase.PaymentTransfer0x0002)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    public async Task PaymentProofAndUnsignedReceipt_DoNotCarryTheSalesLineCount(ReceiptCase receiptCase)
    {
        var (pipeline, _) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);

        var response = await Process(sut, receiptCase, CommandRequestWithLines(receiptCase));

        response.receiptResponse.ftSignatures.Should().NotContain(x => x.Caption == "Nombre de lignes");
    }

    [Fact]
    public async Task TicketWithoutChargeItems_PrintsZeroLines()
    {
        var (pipeline, _) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);

        var response = await sut.PointOfSaleReceipt0x0001Async(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001));

        response.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "Nombre de lignes").Which.Data.Should().Be("0");
    }

    [Fact]
    public async Task CopyReceipt_IsMarkedAsDuplicateAndGoesIntoItsOwnChain()
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new ProtocolCommandProcessorFR(pipeline, new AsyncLazy<IJournalFRCopyPayloadRepository>(() => Task.FromResult<IJournalFRCopyPayloadRepository>(new InMemoryJournalFRCopyPayloadRepository())), new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(new InMemoryQueueItemRepository())));
        var request = CommandRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010);
        request.ReceiptRequest.cbPreviousReceiptReference = "receipt-1";

        var response = await sut.CopyReceiptPrintExistingReceipt0x3010Async(request);

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D1");
        response.receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Duplicata" && x.Data == "1. Duplicata de receipt-1");
        sscd.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task DailyClosing_IsSignedIntoTheDailyClosingChainAndWritesAnActionJournal()
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new DailyOperationsCommandProcessorFR(pipeline);

        var response = await sut.DailyClosing0x2011Async(CommandRequest(ReceiptCase.DailyClosing0x2011));

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#Z1");
        response.actionJournals.Should().ContainSingle();
        sscd.Calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData(ReceiptCase.InitialOperationReceipt0x4001)]
    [InlineData(ReceiptCase.OutOfOperationReceipt0x4002)]
    public async Task TrainingLifecycleReceipt_IsRefusedBeforeSigning_AndLeavesTheQueueAlone(ReceiptCase receiptCase)
    {
        var (pipeline, sscd) = CreatePipeline();
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);
        var request = CommandRequest(receiptCase);
        request.ReceiptRequest.ftReceiptCase = request.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Training);

        var response = receiptCase == ReceiptCase.InitialOperationReceipt0x4001
            ? await sut.InitialOperationReceipt0x4001Async(request)
            : await sut.OutOfOperationReceipt0x4002Async(request);

        response.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        response.actionJournals.Should().BeEmpty("no activation or deactivation is journaled");
        sscd.Calls.Should().BeEmpty("nothing is signed into the training chain");
        storage.Verify(x => x.ActivateQueueAsync(), Times.Never);
        storage.Verify(x => x.DeactivateQueueAsync(), Times.Never);
    }

    [Fact]
    public async Task InitialOperation_WithoutSignatureCreationData_IsRefused()
    {
        var (pipeline, sscd) = CreatePipeline();
        sscd.HasSignatureCreationData = false;
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.InitialOperationReceipt0x4001Async(CommandRequest(ReceiptCase.InitialOperationReceipt0x4001));

        response.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        storage.Verify(x => x.ActivateQueueAsync(), Times.Never, "a French queue may not be started without a certificate");
        sscd.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task InitialOperation_ActivatesTheQueueAndOpensTheTechnicalChain()
    {
        var (pipeline, sscd) = CreatePipeline();
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.InitialOperationReceipt0x4001Async(CommandRequest(ReceiptCase.InitialOperationReceipt0x4001));

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#E1");
        response.actionJournals.Should().ContainSingle();
        storage.Verify(x => x.ActivateQueueAsync(), Times.Once);
    }

    [Fact]
    public async Task OutOfOperation_SignsBeforeDeactivating()
    {
        var (pipeline, sscd) = CreatePipeline();
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var deactivatedAfterSigning = false;
        storage.Setup(x => x.DeactivateQueueAsync()).Callback(() => deactivatedAfterSigning = sscd.Calls.Count == 1).Returns(Task.CompletedTask);
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.OutOfOperationReceipt0x4002Async(CommandRequest(ReceiptCase.OutOfOperationReceipt0x4002));

        deactivatedAfterSigning.Should().BeTrue("a disabled queue must not produce another chain entry");
        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#E1");
    }

    [Fact]
    public async Task InitialOperation_WhenSigningFails_DoesNotActivateTheQueue()
    {
        var (pipeline, sscd) = CreatePipeline();
        sscd.FailWith = "signing unavailable";
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.InitialOperationReceipt0x4001Async(CommandRequest(ReceiptCase.InitialOperationReceipt0x4001));

        response.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        storage.Verify(x => x.ActivateQueueAsync(), Times.Never,
            "an activated queue would accept ordinary receipts without a signed opening entry, and reject the retry as a second initialization");
    }

    [Fact]
    public async Task InitialOperation_WhenTheScuCannotBeReached_DoesNotActivateTheQueue()
    {
        var (pipeline, sscd) = CreatePipeline();
        sscd.ThrowOnProcess = () => new HttpRequestException("connection refused");
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.InitialOperationReceipt0x4001Async(CommandRequest(ReceiptCase.InitialOperationReceipt0x4001));

        response.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        storage.Verify(x => x.ActivateQueueAsync(), Times.Never);
    }

    [Fact]
    public async Task OutOfOperation_WhenSigningFails_LeavesTheQueueOpenSoTheClosingCanBeRetried()
    {
        var (pipeline, sscd) = CreatePipeline();
        sscd.FailWith = "signing unavailable";
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.OutOfOperationReceipt0x4002Async(CommandRequest(ReceiptCase.OutOfOperationReceipt0x4002));

        response.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        storage.Verify(x => x.DeactivateQueueAsync(), Times.Never,
            "a deactivated queue rejects every request, including the retry of this very receipt");
    }

    [Fact]
    public async Task ReceiptValidatorFR_RejectsAnyCurrencyButEuro()
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        var sut = new ReceiptValidatorFR(new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
            Currency = Currency.CHF,
            cbChargeItems = [],
            cbPayItems = [],
        };

        var result = await sut.ValidateAsync(request, new ftQueue());

        result.Errors.Should().Contain(x => x.ErrorCode == "CurrencyMustMatchMarket");
    }

    [Fact]
    public async Task ReceiptValidatorFR_RejectsACopyWithoutAPreviousReceiptReference()
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        var sut = new ReceiptValidatorFR(new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.CopyReceiptPrintExistingReceipt0x3010.WithCountry("FR"),
            Currency = Currency.EUR,
            cbChargeItems = [],
            cbPayItems = [],
        };

        var result = await sut.ValidateAsync(request, new ftQueue());

        result.Errors.Should().Contain(x => x.ErrorCode == "CopyRequiresPreviousReceiptReference", "market-fr#235, as v1.2 did");
    }

    [Fact]
    public async Task ReceiptValidatorFR_AcceptsEuro()
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        var sut = new ReceiptValidatorFR(new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
            Currency = Currency.EUR,
            cbChargeItems = [new ChargeItem { Amount = 1m, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
            cbPayItems = [new PayItem { Amount = 1m, Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }],
        };

        var result = await sut.ValidateAsync(request, new ftQueue());

        result.Errors.Should().NotContain(x => x.ErrorCode == "CurrencyMustMatchMarket");
    }

    /// <summary>
    /// dev only: where #820 meets #860, every sale prints its sales-line count; the delivery note,
    /// neither signed nor numbered, prints nothing (market-fr#221, #148).
    /// </summary>
    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, true)]
    [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003, true)]
    [InlineData(ReceiptCase.ECommerce0x0004, true)]
    [InlineData(ReceiptCase.DeliveryNote0x0005, false)]
    public async Task Sales_PrintTheirLineCount_DeliveryNotesDoNot(ReceiptCase receiptCase, bool printed)
    {
        var (pipeline, _) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);
        var request = CommandRequest(receiptCase);

        var response = receiptCase switch
        {
            ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 => await sut.PointOfSaleReceiptWithoutObligation0x0003Async(request),
            ReceiptCase.ECommerce0x0004 => await sut.ECommerce0x0004Async(request),
            ReceiptCase.DeliveryNote0x0005 => await sut.DeliveryNote0x0005Async(request),
            _ => await sut.PointOfSaleReceipt0x0001Async(request),
        };

        response.receiptResponse.ftSignatures.Any(x => x.Caption == "Nombre de lignes").Should().Be(printed);
    }
}

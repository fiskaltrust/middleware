using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
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
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(Array.Empty<ftQueueItem>());
        return (new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)))), sscd);
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

    [Fact]
    public async Task ReceiptWithoutFiscalizationObligation_IsStoredButNotSigned()
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);

        var response = await sut.PointOfSaleReceiptWithoutObligation0x0003Async(CommandRequest(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003));

        sscd.Calls.Should().BeEmpty("a receipt without obligation must not consume a number of a fiscal chain");
        response.receiptResponse.ftSignatures.Should().ContainSingle().Which.ftSignatureType.IsType(SignatureTypeFR.StoredNotSigned).Should().BeTrue();
    }

    [Theory]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData((ReceiptCase) 0x0007)]
    public async Task ProvisionalDocuments_AreMarkedAndSignedIntoTheBillChain(ReceiptCase receiptCase)
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);
        var request = CommandRequest(receiptCase);

        var response = receiptCase switch
        {
            ReceiptCase.DeliveryNote0x0005 => await sut.DeliveryNote0x0005Async(request),
            (ReceiptCase) 0x0006 => await sut.TableCheck0x0006Async(request),
            _ => await sut.ProForma0x0007Async(request),
        };

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#B1");
        response.receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Document provisoire");
        sscd.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task CopyReceipt_IsMarkedAsDuplicateAndGoesIntoItsOwnChain()
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new ProtocolCommandProcessorFR(pipeline);
        var request = CommandRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010);
        request.ReceiptRequest.cbPreviousReceiptReference = "receipt-1";

        var response = await sut.CopyReceiptPrintExistingReceipt0x3010Async(request);

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D1");
        response.receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Duplicata" && x.Data.Contains("receipt-1"));
        sscd.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task DailyClosing_IsSignedIntoTheGrandTotalChainAndWritesAnActionJournal()
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new DailyOperationsCommandProcessorFR(pipeline);

        var response = await sut.DailyClosing0x2011Async(CommandRequest(ReceiptCase.DailyClosing0x2011));

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#G1");
        response.actionJournals.Should().ContainSingle();
        sscd.Calls.Should().ContainSingle();
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
    public async Task InitialOperation_ActivatesTheQueueAndOpensTheGrandTotalChain()
    {
        var (pipeline, sscd) = CreatePipeline();
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.InitialOperationReceipt0x4001Async(CommandRequest(ReceiptCase.InitialOperationReceipt0x4001));

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#G1");
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
        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#G1");
    }

    [Fact]
    public async Task InitialOperation_WhenSigningFails_DoesNotActivateTheQueue()
    {
        var (pipeline, sscd) = CreatePipeline();
        sscd.ThrowOnProcess = () => new FRSigningUnavailableException();
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.InitialOperationReceipt0x4001Async(CommandRequest(ReceiptCase.InitialOperationReceipt0x4001));

        response.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        storage.Verify(x => x.ActivateQueueAsync(), Times.Never,
            "an activated queue would accept ordinary receipts without a signed opening entry, and reject the retry as a second initialization");
    }

    [Fact]
    public async Task OutOfOperation_WhenSigningFails_LeavesTheQueueOpenSoTheClosingCanBeRetried()
    {
        var (pipeline, sscd) = CreatePipeline();
        sscd.ThrowOnProcess = () => new FRSigningUnavailableException();
        var storage = new Mock<ILocalizedQueueStorageProvider>();
        var sut = new LifecycleCommandProcessorFR(sscd, pipeline, storage.Object);

        var response = await sut.OutOfOperationReceipt0x4002Async(CommandRequest(ReceiptCase.OutOfOperationReceipt0x4002));

        response.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        storage.Verify(x => x.DeactivateQueueAsync(), Times.Never,
            "a deactivated queue rejects every request, including the retry of this very receipt");
    }

    [Fact]
    public void JournalProcessorFR_RefusesFrenchJournalTypesInsteadOfReturningAnEmptyFile()
    {
        var sut = new JournalProcessorFR();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = (JournalType) 0x4652_2000_0000_0001 });

        act.Should().Throw<NotImplementedException>().WithMessage("*not implemented*");
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

    /// <summary>Named like the SCU packages' exception - the queue recognizes it by type name.</summary>
    private class FRSigningUnavailableException : Exception
    {
        public FRSigningUnavailableException() : base("signing unavailable") { }
    }
}

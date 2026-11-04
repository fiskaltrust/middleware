using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class TrainingMentionTests
{
    private static ReceiptRequest Request(ReceiptCase receiptCase) => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        ftReceiptCase = receiptCase,
        Currency = Currency.EUR,
        cbChargeItems = [new() { Amount = 10m, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }],
    };

    private static ReceiptResponse Response() => new()
    {
        ftQueueID = Guid.NewGuid(),
        ftQueueItemID = Guid.NewGuid(),
        ftReceiptIdentification = "ft1#",
        ftReceiptMoment = DateTime.UtcNow,
        ftState = (State) StateFR.Success,
        ftSignatures = [],
    };

    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(Array.Empty<ftQueueItem>());
        return new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
    }

    private static async Task<ReceiptResponse> SignAsync(ReceiptCase receiptCase, FakeFRSSCD? sscd = null)
    {
        var response = await CreatePipeline(sscd ?? new FakeFRSSCD()).SignAsync(new ProcessCommandRequest(new ftQueue { ftQueueId = Guid.NewGuid() }, Request(receiptCase), Response()));
        return response.receiptResponse;
    }

    private static bool IsTrainingMention(SignatureItem signature)
        => signature.Caption == "Mode école" && signature.ftSignatureType == SignatureTypeFR.Information.As<SignatureType>();

    [Fact]
    public void TrainingSignature_CarriesTheSimulationMention()
    {
        var signature = SignaturItemFactory.CreateTrainingSignature();

        signature.ftSignatureFormat.Should().Be(SignatureFormat.Text);
        signature.ftSignatureType.Should().Be(SignatureTypeFR.Information.As<SignatureType>());
        signature.Data.Should().Contain("SIMULATION").And.Contain("NON VALABLE POUR ENCAISSEMENT");
    }

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.ZeroReceipt0x2000)]
    public async Task TrainingReceipt_GetsTheMentionOnce(ReceiptCase receiptCase)
    {
        var response = await SignAsync(receiptCase.WithCountry("FR").WithFlag(ReceiptCaseFlags.Training));

        response.ftSignatures.Where(IsTrainingMention).Should().ContainSingle()
            .Which.Data.Should().Be("SIMULATION - NON VALABLE POUR ENCAISSEMENT");
    }

    [Fact]
    public async Task TrainingMention_IsPresentBeforeTheScuSigns()
    {
        var sscd = new FakeFRSSCD();

        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR").WithFlag(ReceiptCaseFlags.Training), sscd);

        sscd.LastRequest!.ReceiptResponse.ftSignatures.Should().Contain(s => IsTrainingMention(s), "the SCU signs the document as the customer sees it");
    }

    [Fact]
    public async Task NonTrainingReceipt_HasNoMention()
    {
        var response = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"));

        response.ftSignatures.Should().NotContain(s => IsTrainingMention(s));
    }
}

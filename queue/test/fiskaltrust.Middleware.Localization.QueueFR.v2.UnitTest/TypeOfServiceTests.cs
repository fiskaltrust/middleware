using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;
using SharedStateData = fiskaltrust.Middleware.Localization.v2.Models.MiddlewareStateData;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class TypeOfServiceTests
{
    private static ChargeItem Item(ChargeItemCaseTypeOfService typeOfService, decimal amount = 10m)
        => new() { Amount = amount, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithTypeOfService(typeOfService) };

    private static ReceiptRequest Request(params ChargeItem[] chargeItems)
        => new() { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = chargeItems.ToList() };

    private static ReceiptResponse Response() => new()
    {
        ftQueueID = Guid.NewGuid(),
        ftQueueItemID = Guid.NewGuid(),
        ftReceiptIdentification = "ft1#",
        ftReceiptMoment = DateTime.UtcNow,
        ftState = (State) StateFR.Success,
        ftSignatures = [],
    };

    private static (FRSigningPipeline pipeline, FakeFRSSCD sscd) CreatePipeline()
    {
        var sscd = new FakeFRSSCD();
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(Array.Empty<ftQueueItem>());
        return (new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)))), sscd);
    }

    private static string? TypeOfServiceOf(ReceiptResponse response) => MiddlewareStateData.FromReceiptResponse(response).FR?.ftTypeOfService;

    [Fact]
    public void GoodsOnly_IsB()
        => FRTypeOfServiceCalculator.From(Request(Item(ChargeItemCaseTypeOfService.Delivery), Item(ChargeItemCaseTypeOfService.Delivery))).Should().Be("B");

    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.OtherService)]
    [InlineData(ChargeItemCaseTypeOfService.CatalogService)]
    public void ServicesOnly_IsS(ChargeItemCaseTypeOfService service)
        => FRTypeOfServiceCalculator.From(Request(Item(service))).Should().Be("S");

    [Fact]
    public void GoodsAndServices_IsM()
        => FRTypeOfServiceCalculator.From(Request(Item(ChargeItemCaseTypeOfService.Delivery), Item(ChargeItemCaseTypeOfService.CatalogService))).Should().Be("M");

    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.UnknownService)]
    [InlineData(ChargeItemCaseTypeOfService.Tip)]
    [InlineData(ChargeItemCaseTypeOfService.Voucher)]
    [InlineData(ChargeItemCaseTypeOfService.NotOwnSales)]
    [InlineData(ChargeItemCaseTypeOfService.OwnConsumption)]
    [InlineData(ChargeItemCaseTypeOfService.Grant)]
    [InlineData(ChargeItemCaseTypeOfService.Receivable)]
    [InlineData(ChargeItemCaseTypeOfService.CashTransfer)]
    public void OtherItems_NeitherCountNorChangeTheResult(ChargeItemCaseTypeOfService other)
    {
        FRTypeOfServiceCalculator.From(Request(Item(other))).Should().BeNull("a receipt made only of items that are neither goods nor services has no type of service");
        FRTypeOfServiceCalculator.From(Request(Item(other), Item(ChargeItemCaseTypeOfService.Delivery))).Should().Be("B");
        FRTypeOfServiceCalculator.From(Request(Item(other), Item(ChargeItemCaseTypeOfService.OtherService))).Should().Be("S");
    }

    [Fact]
    public void NoChargeItems_HasNoTypeOfService()
    {
        FRTypeOfServiceCalculator.From(Request()).Should().BeNull();
        FRTypeOfServiceCalculator.From(new ReceiptRequest { ftReceiptCase = ReceiptCase.ZeroReceipt0x2000.WithCountry("FR"), Currency = Currency.EUR }).Should().BeNull();
    }

    [Fact]
    public void Refunds_AreClassifiedLikeTheItemsTheyCorrect()
        => FRTypeOfServiceCalculator.From(Request(Item(ChargeItemCaseTypeOfService.Delivery, -10m))).Should().Be("B");

    [Fact]
    public async Task SignedReceipt_CarriesTypeOfServiceInTheFRStateData()
    {
        var (pipeline, _) = CreatePipeline();
        var request = new ProcessCommandRequest(new ftQueue { ftQueueId = Guid.NewGuid() }, Request(Item(ChargeItemCaseTypeOfService.Delivery), Item(ChargeItemCaseTypeOfService.OtherService)), Response());

        var response = await pipeline.SignAsync(request);

        TypeOfServiceOf(response.receiptResponse).Should().Be("M");
    }

    [Fact]
    public async Task SignedReceipt_HandsTheTypeOfServiceToTheScu()
    {
        var (pipeline, sscd) = CreatePipeline();
        var request = new ProcessCommandRequest(new ftQueue { ftQueueId = Guid.NewGuid() }, Request(Item(ChargeItemCaseTypeOfService.Delivery)), Response());

        await pipeline.SignAsync(request);

        TypeOfServiceOf(sscd.LastRequest!.ReceiptResponse).Should().Be("B", "the response is complete before the SCU sees it");
    }

    [Fact]
    public async Task UnsignedReceipt_CarriesTypeOfServiceToo()
    {
        var (pipeline, sscd) = CreatePipeline();
        var sut = new ReceiptCommandProcessorFR(pipeline);
        var request = new ProcessCommandRequest(new ftQueue { ftQueueId = Guid.NewGuid() }, Request(Item(ChargeItemCaseTypeOfService.CatalogService)), Response());
        request.ReceiptRequest.ftReceiptCase = ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003.WithCountry("FR");

        var response = await sut.PointOfSaleReceiptWithoutObligation0x0003Async(request);

        sscd.Calls.Should().BeEmpty();
        TypeOfServiceOf(response.receiptResponse).Should().Be("S");
    }

    [Fact]
    public async Task ReceiptWithoutGoodsOrServices_LeavesTheStateDataUntouched()
    {
        var (pipeline, _) = CreatePipeline();
        var request = new ProcessCommandRequest(new ftQueue { ftQueueId = Guid.NewGuid() }, Request(Item(ChargeItemCaseTypeOfService.Voucher)), Response());

        var response = await pipeline.SignAsync(request);

        response.receiptResponse.ftStateData.Should().BeNull();
    }

    [Fact]
    public void Apply_KeepsThePreviousReceiptReferencesSetBySignProcessor()
    {
        var previous = new Localization.v2.Models.Receipt { Request = Request(), Response = Response() };
        var response = Response();
        response.ftStateData = new SharedStateData { PreviousReceiptReference = [previous] };

        FRTypeOfServiceCalculator.Apply(Request(Item(ChargeItemCaseTypeOfService.Delivery)), response);

        var stateData = MiddlewareStateData.FromReceiptResponse(response);
        stateData.FR!.ftTypeOfService.Should().Be("B");
        stateData.PreviousReceiptReference.Should().ContainSingle().Which.Should().BeSameAs(previous);
    }

    [Fact]
    public void Apply_ReadsStateDataThatArrivedAsJson()
    {
        var response = Response();
        response.ftStateData = JsonSerializer.Deserialize<JsonElement>("{\"FR\":{\"ftTypeOfService\":\"S\"},\"extra\":1}");

        FRTypeOfServiceCalculator.Apply(Request(Item(ChargeItemCaseTypeOfService.Delivery)), response);

        JsonSerializer.Serialize(response.ftStateData).Should().Be("{\"FR\":{\"ftTypeOfService\":\"B\"},\"extra\":1}", "the value is recomputed from the request, the FR block is written once and unknown state data is carried along");
    }

    [Fact]
    public void Apply_LiftsAnFRBlockOutOfTheSharedExtensionData()
    {
        var response = Response();
        response.ftStateData = JsonSerializer.Deserialize<SharedStateData>("{\"FR\":{\"ftTypeOfService\":\"S\"}}");

        FRTypeOfServiceCalculator.Apply(Request(Item(ChargeItemCaseTypeOfService.OtherService), Item(ChargeItemCaseTypeOfService.Delivery)), response);

        JsonSerializer.Serialize(response.ftStateData).Should().Be("{\"FR\":{\"ftTypeOfService\":\"M\"}}");
    }

    [Fact]
    public void StateData_SerializesAsAnFRBlock()
    {
        var response = Response();

        FRTypeOfServiceCalculator.Apply(Request(Item(ChargeItemCaseTypeOfService.Delivery), Item(ChargeItemCaseTypeOfService.OtherService)), response);

        JsonSerializer.Serialize(response.ftStateData).Should().Be("{\"FR\":{\"ftTypeOfService\":\"M\"}}");
    }
}

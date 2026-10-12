using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
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

public class VatSummaryTests
{
    private static Dictionary<string, FRVatRateSummary> Summary() => new()
    {
        ["20"] = new FRVatRateSummary { AmountHT = 10.00m, VATAmount = 2.00m, AmountTTC = 12.00m },
        ["5.5"] = new FRVatRateSummary { AmountHT = 5.21m, VATAmount = 0.29m, AmountTTC = 5.50m },
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

    private static ProcessCommandRequest CommandRequest() => new(
        new ftQueue { ftQueueId = Guid.NewGuid() },
        new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
            Currency = Currency.EUR,
            cbChargeItems =
            [
                new ChargeItem { Amount = 12.00m, VATRate = 20m, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) },
            ],
        },
        Response());

    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(Array.Empty<ftQueueItem>());
        return new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
    }

    [Fact]
    public async Task SignAsync_CarriesTheSummaryTheSCUReturnedIntoTheFRStateData()
    {
        var sscd = new FakeFRSSCD { VatSummary = Summary() };

        var result = await CreatePipeline(sscd).SignAsync(CommandRequest());

        var fr = MiddlewareStateData.FromReceiptResponse(result.receiptResponse).FR!;
        fr.VATSummary.Should().BeEquivalentTo(Summary());
        fr.ftTypeOfService.Should().Be("B", "the summary is added next to what the queue already put in the FR block");
    }

    [Fact]
    public async Task SignAsync_LeavesTheStateDataWithoutSummaryWhenTheSCUReturnsNone()
    {
        var result = await CreatePipeline(new FakeFRSSCD()).SignAsync(CommandRequest());

        JsonSerializer.Serialize(result.receiptResponse.ftStateData).Should().Be("{\"FR\":{\"ftTypeOfService\":\"B\"}}");
    }

    [Fact]
    public async Task SignAsync_WritesNoSummaryWhenSigningFails()
    {
        var sscd = new FakeFRSSCD { VatSummary = Summary(), ThrowOnProcess = () => new FRSigningUnavailableException() };

        var result = await CreatePipeline(sscd).SignAsync(CommandRequest());

        MiddlewareStateData.FromReceiptResponse(result.receiptResponse).FR?.VATSummary.Should().BeNull();
    }

    [Fact]
    public void Apply_KeepsThePreviousReceiptReferencesSetBySignProcessor()
    {
        var previous = new Localization.v2.Models.Receipt { Request = new ReceiptRequest(), Response = Response() };
        var response = Response();
        response.ftStateData = new SharedStateData { PreviousReceiptReference = [previous] };

        FRVatSummary.Apply(response, Summary());

        var stateData = MiddlewareStateData.FromReceiptResponse(response);
        stateData.FR!.VATSummary.Should().ContainKeys("20", "5.5");
        stateData.PreviousReceiptReference.Should().ContainSingle().Which.Should().BeSameAs(previous);
    }

    [Fact]
    public void Apply_DoesNothingWithoutSummary()
    {
        var response = Response();

        FRVatSummary.Apply(response, null);

        response.ftStateData.Should().BeNull();
    }

    [Fact]
    public void StateData_SerializesTheSummaryKeyedByRate()
    {
        var response = Response();

        FRVatSummary.Apply(response, new Dictionary<string, FRVatRateSummary> { ["5.5"] = new() { AmountHT = 5.21m, VATAmount = 0.29m, AmountTTC = 5.50m } });

        JsonSerializer.Serialize(response.ftStateData).Should().Be("{\"FR\":{\"VATSummary\":{\"5.5\":{\"AmountHT\":5.21,\"VATAmount\":0.29,\"AmountTTC\":5.50}}}}");
    }

    [Fact]
    public void StateData_ReadsTheSummaryBackFromJson()
    {
        var response = Response();
        response.ftStateData = JsonSerializer.Deserialize<JsonElement>("{\"FR\":{\"VATSummary\":{\"20\":{\"AmountHT\":10.00,\"VATAmount\":2.00,\"AmountTTC\":12.00}}}}");

        var fr = MiddlewareStateData.FromReceiptResponse(response).FR!;

        fr.VATSummary!["20"].AmountHT.Should().Be(10.00m);
    }

    /// <summary>Named like the SCU packages' exception, which the queue recognizes by type name.</summary>
    private class FRSigningUnavailableException : Exception
    {
        public FRSigningUnavailableException() : base("signing unavailable") { }
    }
}

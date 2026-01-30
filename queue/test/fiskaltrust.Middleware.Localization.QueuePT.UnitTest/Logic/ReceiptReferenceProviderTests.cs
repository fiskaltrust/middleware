using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Logic;

public class ReceiptReferenceProviderTests
{
    [Fact]
    public async Task GetChargeItemMatchesForPreviousReferenceAsync_WithMatch_ReturnsChargeItemMatch()
    {
        var now = DateTime.UtcNow;
        var matchRequest = CreateRequest("IGNORED", "Coffee", now, "REF-1");
        var matchResponse = CreateResponse("REF-1", now);

        var otherRequest = CreateRequest("REF-1", "Tea", now, "R-002");
        var otherResponse = CreateResponse("R-002", now);

        var differentReferenceRequest = CreateRequest("REF-2", "Coffee", now, "R-003");
        var differentReferenceResponse = CreateResponse("R-003", now);

        var queueItems = new List<ftQueueItem>
        {
            CreateQueueItem(matchRequest, matchResponse),
            CreateQueueItem(otherRequest, otherResponse),
            CreateQueueItem(differentReferenceRequest, differentReferenceResponse)
        };

        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(queueItems.ToAsyncEnumerable());

        var provider = new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));

        var results = await provider.GetChargeItemMatchesForPreviousReferenceAsync(
            "REF-1",
            new List<ChargeItem>
            {
                new()
                {
                    Description = "Coffee",
                    Quantity = 1m,
                    Amount = 10m,
                    VATRate = 23m,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate
                }
            });

        results.Should().HaveCount(1);
        results[0].ReferencedReceipt.Request.cbReceiptReference.Should().Be("REF-1");
        results[0].ReferencedChargeItem.Description.Should().Be("Coffee");
    }

    [Fact]
    public async Task GetChargeItemMatchesForPreviousReferenceAsync_NoDescriptionMatch_ReturnsEmpty()
    {
        var now = DateTime.UtcNow;
        var request = CreateRequest("IGNORED", "Coffee", now, "REF-1");
        var response = CreateResponse("REF-1", now);

        var queueItems = new List<ftQueueItem> { CreateQueueItem(request, response) };

        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(queueItems.ToAsyncEnumerable());

        var provider = new ReceiptReferenceProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));

        var results = await provider.GetChargeItemMatchesForPreviousReferenceAsync(
            "REF-1",
            new List<ChargeItem>
            {
                new()
                {
                    Description = "Tea",
                    Quantity = 1m,
                    Amount = 10m,
                    VATRate = 23m,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate
                }
            });

        results.Should().BeEmpty();
    }

    private static ReceiptRequest CreateRequest(string previousReference, string description, DateTime moment, string receiptReference)
    {
        return new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbPreviousReceiptReference = previousReference,
            cbReceiptReference = receiptReference,
            cbTerminalID = "TERM-001",
            cbUser = "user",
            cbReceiptMoment = moment,
            Currency = Currency.EUR,
            cbChargeItems = new List<ChargeItem>
            {
                new()
                {
                    Position = 1,
                    Description = description,
                    Quantity = 1m,
                    Amount = 10m,
                    VATRate = 23m,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate
                }
            },
            cbPayItems = new List<PayItem>()
        };
    }

    private static ReceiptResponse CreateResponse(string receiptIdentification, DateTime moment)
    {
        return new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = receiptIdentification,
            ftReceiptMoment = moment,
            ftState = State.Success
        };
    }

    private static ftQueueItem CreateQueueItem(ReceiptRequest request, ReceiptResponse response)
    {
        return new ftQueueItem
        {
            request = JsonSerializer.Serialize(request),
            response = JsonSerializer.Serialize(response)
        };
    }
}

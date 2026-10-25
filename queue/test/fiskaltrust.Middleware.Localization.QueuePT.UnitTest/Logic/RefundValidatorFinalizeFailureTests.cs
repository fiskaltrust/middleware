using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Logic;

/// <summary>
/// RFC 712: a partial refund whose finalize call failed after fiscalization carries the error state next to the
/// FiscalizationSucceeded marker; the refund limit must still count it, or the same amount can be refunded again.
/// </summary>
public class RefundValidatorFinalizeFailureTests
{
    private static readonly Guid _cashBoxId = Guid.NewGuid();

    [Fact]
    public async Task APartialRefundWhoseFinalizeCallFailed_StillCountsAgainstTheRefundableAmount()
    {
        var sale = Sale("S-1", 100m);
        var firstRefund = PartialRefund("R-1", sale, 60m);
        var (_, firstRefundResponse) = Fiscalized("ftD#NC ft20250a63/1");
        firstRefundResponse.ftState = firstRefundResponse.ftState.WithState(State.Error);
        firstRefundResponse.ftStateData = new MiddlewareStateData { PostFiscalization = new PostFiscalizationStateData { FiscalizationSucceeded = true, EReporting = PostFiscalizationStateData.Failed } };
        var validator = Validator(QueueItem(sale, Fiscalized("ftD#FT ft20250a62/1").response), QueueItem(firstRefund, firstRefundResponse));

        var error = await validator.ValidatePartialRefundAsync(PartialRefund("R-2", sale, 60m), sale, "S-1");

        error.Should().NotBeNull("60 of 100 were already refunded by a fiscalized refund, so another 60 exceed the original amount");
        error.Should().Contain("exceeds original amount");
    }

    [Fact]
    public async Task APartialRefundThatWasNeverFiscalized_DoesNotCountAgainstTheRefundableAmount()
    {
        var sale = Sale("S-1", 100m);
        var firstRefund = PartialRefund("R-1", sale, 60m);
        var (_, firstRefundResponse) = Fiscalized("ftD#NC ft20250a63/1");
        firstRefundResponse.ftState = firstRefundResponse.ftState.WithState(State.Error); // no FiscalizationSucceeded marker: a genuine failure
        var validator = Validator(QueueItem(sale, Fiscalized("ftD#FT ft20250a62/1").response), QueueItem(firstRefund, firstRefundResponse));

        var error = await validator.ValidatePartialRefundAsync(PartialRefund("R-2", sale, 60m), sale, "S-1");

        error.Should().BeNull("the first refund refunded nothing");
    }

    private static RefundValidator Validator(params ftQueueItem[] queueItems)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>())).Returns(QueueItems(queueItems));
        return new RefundValidator(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));
    }

    private static async IAsyncEnumerable<ftQueueItem> QueueItems(ftQueueItem[] queueItems)
    {
        foreach (var queueItem in queueItems)
        {
            yield return queueItem;
        }
        await Task.CompletedTask;
    }

    private static ftQueueItem QueueItem(ReceiptRequest request, ReceiptResponse response) => new()
    {
        ftQueueItemId = Guid.NewGuid(),
        cbReceiptReference = request.cbReceiptReference,
        request = JsonSerializer.Serialize(request),
        response = JsonSerializer.Serialize(response),
    };

    private static ReceiptRequest Sale(string reference, decimal amount) => new()
    {
        ftCashBoxID = _cashBoxId,
        cbReceiptReference = reference,
        cbReceiptMoment = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc),
        ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_1001,
        Currency = Currency.EUR,
        cbChargeItems = [new ChargeItem { Position = 1, Quantity = 1, Description = "Line item", Amount = amount, VATRate = 23m, ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013 }],
        cbPayItems = [new PayItem { Position = 1, Description = "Cash", Amount = amount, ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001 }],
        cbUser = "Operator",
    };

    /// <summary>A partial refund of the sale: the same item at the same unit price, a fraction of its quantity, amounts negated.</summary>
    private static ReceiptRequest PartialRefund(string reference, ReceiptRequest sale, decimal amount)
    {
        var fraction = amount / sale.cbChargeItems[0].Amount;
        return new ReceiptRequest
        {
            ftCashBoxID = sale.ftCashBoxID,
            cbReceiptReference = reference,
            cbPreviousReceiptReference = sale.cbReceiptReference,
            cbReceiptMoment = sale.cbReceiptMoment.AddHours(1),
            ftReceiptCase = sale.ftReceiptCase,
            Currency = sale.Currency,
            cbChargeItems = [new ChargeItem { Position = 1, Quantity = -fraction, Description = "Line item", Amount = -amount, VATRate = 23m, ftChargeItemCase = ((ChargeItemCase) 0x5054_2000_0000_0013).WithFlag(ChargeItemCaseFlags.Refund) }],
            cbPayItems = [new PayItem { Position = 1, Description = "Cash", Amount = -amount, ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001 }],
            cbUser = "Operator",
        };
    }

    private static (ReceiptRequest request, ReceiptResponse response) Fiscalized(string receiptIdentification) => (new ReceiptRequest(), new ReceiptResponse
    {
        ftReceiptIdentification = receiptIdentification,
        ftReceiptMoment = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc),
        ftState = State.Success,
        ftSignatures = [],
    });
}

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
/// RFC 712: a referencing document whose finalize call failed after fiscalization carries the error state next to the
/// FiscalizationSucceeded marker; the document status must still see it, or the SAF-T export reports a voided invoice as
/// normal and keeps its amounts in the totals.
/// </summary>
public class DocumentStatusProviderFinalizeFailureTests
{
    private const ReceiptCase InvoiceCase = (ReceiptCase) 0x5054_2000_0000_1001;
    private const ReceiptCase VoidCase = (ReceiptCase) 0x5054_2000_0004_1001; // the invoice case with the void flag

    [Fact]
    public async Task AVoidWhoseFinalizeCallFailed_StillCancelsTheInvoiceItReferences()
    {
        var (invoiceRequest, invoiceResponse) = Document("ftD#FT ft20250a62/1", InvoiceCase, new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc));
        var (voidRequest, voidResponse) = Document("ftD#NC ft20250a63/1", VoidCase, new DateTime(2026, 9, 10, 11, 0, 0, DateTimeKind.Utc), previousReceiptReference: invoiceRequest.cbReceiptReference);
        voidResponse.ftState = voidResponse.ftState.WithState(State.Error);
        voidResponse.ftStateData = new MiddlewareStateData { PostFiscalization = new PostFiscalizationStateData { FiscalizationSucceeded = true, EReporting = PostFiscalizationStateData.Failed } };
        var provider = Provider(QueueItem(invoiceRequest, invoiceResponse), QueueItem(voidRequest, voidResponse));

        var status = await provider.GetDocumentStatusStateAsync((invoiceRequest, invoiceResponse));

        status.Status.Should().Be(DocumentStatus.Voided, "the void was fiscalized; only its finalize call failed");
    }

    [Fact]
    public async Task AVoidThatWasNeverFiscalized_DoesNotCancelTheInvoiceItReferences()
    {
        var (invoiceRequest, invoiceResponse) = Document("ftD#FT ft20250a62/1", InvoiceCase, new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc));
        var (voidRequest, voidResponse) = Document("ftD#NC ft20250a63/1", VoidCase, new DateTime(2026, 9, 10, 11, 0, 0, DateTimeKind.Utc), previousReceiptReference: invoiceRequest.cbReceiptReference);
        voidResponse.ftState = voidResponse.ftState.WithState(State.Error); // no FiscalizationSucceeded marker: a genuine failure
        var provider = Provider(QueueItem(invoiceRequest, invoiceResponse), QueueItem(voidRequest, voidResponse));

        var status = await provider.GetDocumentStatusStateAsync((invoiceRequest, invoiceResponse));

        status.Status.Should().Be(DocumentStatus.NotReferenced);
    }

    private static DocumentStatusProvider Provider(params ftQueueItem[] queueItems)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>())).Returns(QueueItems(queueItems));
        return new DocumentStatusProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)));
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

    private static (ReceiptRequest request, ReceiptResponse response) Document(string receiptIdentification, ReceiptCase receiptCase, DateTime moment, string? previousReceiptReference = null)
    {
        var request = new ReceiptRequest
        {
            cbReceiptReference = receiptIdentification,
            cbReceiptMoment = moment,
            ftReceiptCase = receiptCase,
            cbChargeItems = [new ChargeItem { Quantity = 1, Description = "Line item", Amount = 100m, VATRate = 23m, ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013 }],
            cbPayItems = [new PayItem { Description = "Cash", Amount = 100m, ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001 }],
            cbUser = "Operator",
        };
        if (previousReceiptReference is not null)
        {
            request.cbPreviousReceiptReference = previousReceiptReference;
        }

        var response = new ReceiptResponse
        {
            ftReceiptIdentification = receiptIdentification,
            ftReceiptMoment = moment,
            ftState = State.Success,
            ftSignatures = [],
        };
        return (request, response);
    }
}

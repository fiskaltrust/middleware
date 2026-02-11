using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Processors;

public class InvoiceCommandProcessorRefundTests
{
    private readonly Mock<IPTSSCD> _mockSscd;
    private readonly ftQueuePT _queuePT;
    private readonly Mock<IMiddlewareQueueItemRepository> _mockQueueItemRepository;
    private readonly InvoiceCommandProcessorPT _processor;

    public InvoiceCommandProcessorRefundTests()
    {
        _mockSscd = new Mock<IPTSSCD>();
        _queuePT = new ftQueuePT
        {
            ftQueuePTId = Guid.NewGuid(),
            IssuerTIN = "123456789",
            NumeratorStorage = new NumeratorStorage
            {
                InvoiceSeries = new NumberSeries
                {
                    TypeCode = "FT",
                    ATCUD = "ATCUD-123",
                    Series = "2024",
                    Numerator = 0,
                    LastHash = "initial-hash"
                },
                CreditNoteSeries = new NumberSeries
                {
                    TypeCode = "NC",
                    ATCUD = "ATCUD-456",
                    Series = "2024",
                    Numerator = 0,
                    LastHash = "initial-hash"
                }
            }
        };

        _mockQueueItemRepository = new Mock<IMiddlewareQueueItemRepository>();

        var asyncLazy = new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(_mockQueueItemRepository.Object));

        _processor = new InvoiceCommandProcessorPT(
            _mockSscd.Object,
            _queuePT,
            asyncLazy,
            true
        );

        // Setup mock SSCD to return valid response
        _mockSscd.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>(), It.IsAny<string>()))
            .ReturnsAsync((ProcessRequest req, string lastHash) =>
            {
                return new ValueTuple<ProcessResponse, string>(
                    new ProcessResponse { ReceiptResponse = req.ReceiptResponse },
                    "0123456789012345678901234567890123456789"
                );
            });
    }

    private ReceiptRequest CreateInvoiceRequest(string receiptReference, params ChargeItem[] chargeItems)
    {
        return new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("PT"),
            cbReceiptReference = receiptReference,
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = chargeItems.ToList(),
            cbPayItems = []
        };
    }

    private ReceiptResponse CreateReceiptResponse()
    {
        return new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 1,
            ftReceiptIdentification = "receipt-id",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) 0x5054_0000_0000_0000
        };
    }

    private void SetupQueueItemRepository(List<ftQueueItem> items)
    {
        _mockQueueItemRepository.Setup(x => x.GetByReceiptReferenceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string receiptRef, string terminalId) =>
            {
                var matchingItems = items.Where(i => i.cbReceiptReference == receiptRef);
                if (!string.IsNullOrEmpty(terminalId))
                {
                    matchingItems = matchingItems.Where(i => i.cbTerminalID == terminalId);
                }
                return matchingItems.ToAsyncEnumerable();
            });

        _mockQueueItemRepository.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(items.ToAsyncEnumerable());
    }
}

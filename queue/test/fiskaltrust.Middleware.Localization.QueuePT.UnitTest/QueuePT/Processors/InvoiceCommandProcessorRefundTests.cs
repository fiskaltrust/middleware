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
            asyncLazy
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
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = receiptReference,
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = chargeItems.ToList()
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
            ftState = (State)0x5054_0000_0000_0000
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

    #region Full Refund Tests

    [Fact]
    public async Task InvoiceB2C0x1001Async_FullRefund_WithValidItems_ShouldSucceed()
    {
        // Arrange - Create and store original invoice
        var originalRequest = CreateInvoiceRequest("INV-001",
            new ChargeItem
            {
                ProductNumber = "PROD-001",
                Description = "Product 1",
                Quantity = 2,
                Amount = 100,
                VATRate = 23m,
                ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
            });

        var queueItems = new List<ftQueueItem>
        {
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-001",
                cbTerminalID = "TERM-001",
                request = JsonSerializer.Serialize(originalRequest),
                response = JsonSerializer.Serialize(CreateReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks
            }
        };
        SetupQueueItemRepository(queueItems);

        // Create full refund request
        var refundRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.InvoiceB2C0x1001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-001",
            cbReceiptReference = "REF-001",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -2,
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        var refundResponse = CreateReceiptResponse();
        var request = new ProcessCommandRequest(new ftQueue(), refundRequest, refundResponse);

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(request);

        // Assert
        result.receiptResponse.Should().NotBeNull();
        result.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE);
    }

    [Fact]
    public async Task InvoiceB2C0x1001Async_FullRefund_WithMissingItem_ShouldReturnError()
    {
        // Arrange - Create and store original invoice with 2 products
        var originalRequest = CreateInvoiceRequest("INV-002",
            new ChargeItem
            {
                ProductNumber = "PROD-001",
                Description = "Product 1",
                Quantity = 2,
                Amount = 100,
                VATRate = 23m,
                ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
            },
            new ChargeItem
            {
                ProductNumber = "PROD-002",
                Description = "Product 2",
                Quantity = 1,
                Amount = 50,
                VATRate = 23m,
                ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
            });

        var queueItems = new List<ftQueueItem>
        {
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-002",
                cbTerminalID = "TERM-001",
                request = JsonSerializer.Serialize(originalRequest),
                response = JsonSerializer.Serialize(CreateReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks
            }
        };
        SetupQueueItemRepository(queueItems);

        // Create full refund request with only 1 product (missing PROD-002)
        var refundRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.InvoiceB2C0x1001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-002",
            cbReceiptReference = "REF-002",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -2,
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        var refundResponse = CreateReceiptResponse();
        var request = new ProcessCommandRequest(new ftQueue(), refundRequest, refundResponse);

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(request);

        // Assert
        result.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE);
        result.receiptResponse.ftStateData.ToString().Should().Contain(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-002"));
    }

    [Fact]
    public async Task InvoiceB2C0x1001Async_FullRefund_SecondRefundAttempt_ShouldReturnError()
    {
        // Arrange - Create and store original invoice
        var originalRequest = CreateInvoiceRequest("INV-003",
            new ChargeItem
            {
                ProductNumber = "PROD-001",
                Description = "Product 1",
                Quantity = 2,
                Amount = 100,
                VATRate = 23m,
                ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
            });

        // Store first refund
        var firstRefund = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.InvoiceB2C0x1001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-003",
            cbReceiptReference = "REF-003-1",
            cbTerminalID = "TERM-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Quantity = -2,
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        var queueItems = new List<ftQueueItem>
        {
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-003",
                cbTerminalID = "TERM-001",
                request = JsonSerializer.Serialize(originalRequest),
                response = JsonSerializer.Serialize(CreateReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 10000
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "REF-003-1",
                cbTerminalID = "TERM-001",
                request = JsonSerializer.Serialize(firstRefund),
                response = JsonSerializer.Serialize(CreateReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 5000
            }
        };
        SetupQueueItemRepository(queueItems);

        // Attempt second refund
        var secondRefund = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.InvoiceB2C0x1001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-003",
            cbReceiptReference = "REF-003-2",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Quantity = -2,
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        var refundResponse = CreateReceiptResponse();
        var request = new ProcessCommandRequest(new ftQueue(), secondRefund, refundResponse);

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(request);

        // Assert
        result.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE);
        result.receiptResponse.ftStateData.ToString().Should().Contain(ErrorMessagesPT.EEEE_RefundAlreadyExists("INV-003"));
    }

    #endregion

    #region Partial Refund Tests

    [Fact]
    public async Task InvoiceB2C0x1001Async_PartialRefund_WithValidItems_ShouldSucceed()
    {
        // Arrange - Create and store original invoice
        var originalRequest = CreateInvoiceRequest("INV-004",
            new ChargeItem
            {
                ProductNumber = "PROD-001",
                Description = "Product 1",
                Quantity = 5,
                Amount = 250,
                VATRate = 23m,
                ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
            });

        var queueItems = new List<ftQueueItem>
        {
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-004",
                cbTerminalID = "TERM-001",
                request = JsonSerializer.Serialize(originalRequest),
                response = JsonSerializer.Serialize(CreateReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks
            }
        };
        SetupQueueItemRepository(queueItems);

        // Create partial refund request (no refund flag on receipt case, but on items)
        var partialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001, // No refund flag
            cbPreviousReceiptReference = "INV-004",
            cbReceiptReference = "PREF-004",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -2, // Partial quantity
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                }
            ]
        };

        var refundResponse = CreateReceiptResponse();
        var request = new ProcessCommandRequest(new ftQueue(), partialRefundRequest, refundResponse);

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(request);

        // Assert
        result.receiptResponse.Should().NotBeNull();
        result.receiptResponse.ftState.Should().NotBe((State)0x5054_0000_EEEE_EEEE);
    }

    [Fact]
    public async Task InvoiceB2C0x1001Async_PartialRefund_WithMixedItems_ShouldReturnError()
    {
        // Arrange - Create and store original invoice
        var originalRequest = CreateInvoiceRequest("INV-005",
            new ChargeItem
            {
                ProductNumber = "PROD-001",
                Description = "Product 1",
                Quantity = 5,
                Amount = 250,
                VATRate = 23m,
                ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
            });

        var queueItems = new List<ftQueueItem>
        {
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-005",
                cbTerminalID = "TERM-001",
                request = JsonSerializer.Serialize(originalRequest),
                response = JsonSerializer.Serialize(CreateReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks
            }
        };
        SetupQueueItemRepository(queueItems);

        // Create request with mixed refund/non-refund items
        var mixedRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-005",
            cbReceiptReference = "MIX-005",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -1,
                    Amount = -50,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund) // Has refund flag
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Product 2",
                    Quantity = 1,
                    Amount = 50,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase // No refund flag - mixed!
                }
            ]
        };

        var response = CreateReceiptResponse();
        var request = new ProcessCommandRequest(new ftQueue(), mixedRequest, response);

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(request);

        // Assert
        result.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE);
        result.receiptResponse.ftStateData.ToString().Should().Contain(ErrorMessagesPT.EEEE_MixedRefundItemsNotAllowed);
    }

    [Fact]
    public async Task InvoiceB2C0x1001Async_RefundWithoutPreviousReference_ShouldReturnError()
    {
        // Arrange - Create partial refund without cbPreviousReceiptReference
        SetupQueueItemRepository(new List<ftQueueItem>());
        
        var partialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            // No cbPreviousReceiptReference!
            cbReceiptReference = "PREF-008",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -2,
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                }
            ]
        };

        var response = CreateReceiptResponse();
        var request = new ProcessCommandRequest(new ftQueue(), partialRefundRequest, response);

        // Act
        var result = await _processor.InvoiceB2C0x1001Async(request);

        // Assert
        result.receiptResponse.ftState.Should().Be((State)0x5054_0000_EEEE_EEEE);
        result.receiptResponse.ftStateData.ToString().Should().Contain(ErrorMessagesPT.EEEE_MixedRefundItemsNotAllowed);
    }

    #endregion
}

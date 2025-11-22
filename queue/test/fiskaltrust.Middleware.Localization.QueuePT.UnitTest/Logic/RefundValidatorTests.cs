using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using System.Text.Json;
using Xunit;
using Moq;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Logic;

public class RefundValidatorTests
{
    private RefundValidator CreateValidator(IMiddlewareQueueItemRepository repository)
    {
        return new RefundValidator(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository)));
    }

    private Mock<IMiddlewareQueueItemRepository> CreateMockRepository(List<ftQueueItem>? queueItems = null)
    {
        var mock = new Mock<IMiddlewareQueueItemRepository>();
        var items = queueItems ?? new List<ftQueueItem>();

        mock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(items.ToAsyncEnumerable());

        return mock;
    }

    private static ReceiptRequest CreateOriginalInvoice(decimal quantity1 = 2, decimal amount1 = 100, decimal quantity2 = 1, decimal amount2 = 50)
    {
        return new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "INV-001",
            cbTerminalID = "TERM-001",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = quantity1,
                    Amount = amount1,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Product 2",
                    Quantity = quantity2,
                    Amount = amount2,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };
    }

    #region Full Refund Validation Tests

    [Fact]
    public async Task ValidateFullRefund_WithMatchingItems_ShouldPass()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice();
        var refundRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-001",
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
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Product 2",
                    Quantity = -1,
                    Amount = -50,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        // Act
        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateFullRefund_WithMissingItem_ShouldReturnError()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice();
        var refundRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-001",
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
                // Missing PROD-002
            ]
        };

        // Act
        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001"));
    }

    [Fact]
    public async Task ValidateFullRefund_WithExtraItem_ShouldReturnError()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice();
        var refundRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-001",
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
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Product 2",
                    Quantity = -1,
                    Amount = -50,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-003",
                    Description = "Extra Product",
                    Quantity = -1,
                    Amount = -25,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        // Act
        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001"));
    }

    [Fact]
    public async Task ValidateFullRefund_WithIncorrectQuantity_ShouldReturnError()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice();
        var refundRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -3, // Wrong quantity (should be -2)
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Product 2",
                    Quantity = -1,
                    Amount = -50,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        // Act
        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001"));
    }

    [Fact]
    public async Task ValidateFullRefund_WithIncorrectAmount_ShouldReturnError()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice();
        var refundRequest = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -2,
                    Amount = -90, // Wrong amount (should be -100)
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                },
                new ChargeItem
                {
                    ProductNumber = "PROD-002",
                    Description = "Product 2",
                    Quantity = -1,
                    Amount = -50,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
                }
            ]
        };

        // Act
        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001"));
    }

    #endregion

    #region Partial Refund Validation Tests

    [Fact]
    public async Task ValidatePartialRefund_WithAllRefundFlagsSet_ShouldPass()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice(quantity1: 5, amount1: 250);
        var partialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001, // No refund flag on receipt case
            cbPreviousReceiptReference = "INV-001",
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

        // Act
        var result = await validator.ValidatePartialRefundAsync(partialRefundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidatePartialRefund_WithMixedRefundFlags_ShouldReturnError()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice();
        var partialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-001",
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

        // Act
        var result = await validator.ValidatePartialRefundAsync(partialRefundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Be(ErrorMessagesPT.EEEE_MixedRefundItemsNotAllowed);
    }

    [Fact]
    public async Task ValidatePartialRefund_ExceedingOriginalQuantity_ShouldReturnError()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice(quantity1: 2, amount1: 100);
        var partialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -3, // Exceeds original quantity of 2
                    Amount = -100,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                }
            ]
        };

        // Act
        var result = await validator.ValidatePartialRefundAsync(partialRefundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Contain("exceeds the original quantity");
        result.Should().Contain("PROD-001");
    }

    [Fact]
    public async Task ValidatePartialRefund_ExceedingOriginalAmount_ShouldReturnError()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice(quantity1: 2, amount1: 100);
        var partialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -2,
                    Amount = -120, // Exceeds original amount of 100
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                }
            ]
        };

        // Act
        var result = await validator.ValidatePartialRefundAsync(partialRefundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Contain("exceeds the original amount");
        result.Should().Contain("PROD-001");
    }

    [Fact]
    public async Task ValidatePartialRefund_WithMultiplePartialRefunds_ShouldValidateTotalQuantity()
    {
        // Arrange
        var queueItems = new List<ftQueueItem>
        {
            // Original invoice
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-001",
                request = JsonSerializer.Serialize(CreateOriginalInvoice(quantity1: 5, amount1: 250)),
                response = JsonSerializer.Serialize(new ReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 10000
            },
            // First partial refund
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "REF-001",
                request = JsonSerializer.Serialize(new ReceiptRequest
                {
                    ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
                    cbPreviousReceiptReference = "INV-001",
                    cbChargeItems =
                    [
                        new ChargeItem
                        {
                            ProductNumber = "PROD-001",
                            Quantity = -2,
                            Amount = -100,
                            VATRate = 23m,
                            ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                        }
                    ]
                }),
                response = JsonSerializer.Serialize(new ReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 5000
            }
        };

        var mockRepo = CreateMockRepository(queueItems);
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice(quantity1: 5, amount1: 250);

        // Second partial refund that would exceed total
        var secondPartialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -4, // Total would be 6, exceeding original 5
                    Amount = -200,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                }
            ]
        };

        // Act
        var result = await validator.ValidatePartialRefundAsync(secondPartialRefundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Contain("exceeds the original quantity");
        result.Should().Contain("PROD-001");
    }

    [Fact]
    public async Task ValidatePartialRefund_WithMultiplePartialRefunds_ShouldValidateTotalAmount()
    {
        // Arrange
        var queueItems = new List<ftQueueItem>
        {
            // Original invoice
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-001",
                request = JsonSerializer.Serialize(CreateOriginalInvoice(quantity1: 5, amount1: 250)),
                response = JsonSerializer.Serialize(new ReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 10000
            },
            // First partial refund
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "REF-001",
                request = JsonSerializer.Serialize(new ReceiptRequest
                {
                    ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
                    cbPreviousReceiptReference = "INV-001",
                    cbChargeItems =
                    [
                        new ChargeItem
                        {
                            ProductNumber = "PROD-001",
                            Quantity = -2,
                            Amount = -100,
                            VATRate = 23m,
                            ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                        }
                    ]
                }),
                response = JsonSerializer.Serialize(new ReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 5000
            }
        };

        var mockRepo = CreateMockRepository(queueItems);
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice(quantity1: 5, amount1: 250);

        // Second partial refund that would exceed total amount
        var secondPartialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -3,
                    Amount = -160, // Total would be 260, exceeding original 250
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                }
            ]
        };

        // Act
        var result = await validator.ValidatePartialRefundAsync(secondPartialRefundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().Contain("exceeds the original amount");
        result.Should().Contain("PROD-001");
    }

    [Fact]
    public async Task ValidatePartialRefund_WithMultipleValidPartialRefunds_ShouldPass()
    {
        // Arrange
        var queueItems = new List<ftQueueItem>
        {
            // Original invoice
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "INV-001",
                request = JsonSerializer.Serialize(CreateOriginalInvoice(quantity1: 5, amount1: 250)),
                response = JsonSerializer.Serialize(new ReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 10000
            },
            // First partial refund
            new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                cbReceiptReference = "REF-001",
                request = JsonSerializer.Serialize(new ReceiptRequest
                {
                    ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
                    cbPreviousReceiptReference = "INV-001",
                    cbChargeItems =
                    [
                        new ChargeItem
                        {
                            ProductNumber = "PROD-001",
                            Quantity = -2,
                            Amount = -100,
                            VATRate = 23m,
                            ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                        }
                    ]
                }),
                response = JsonSerializer.Serialize(new ReceiptResponse()),
                TimeStamp = DateTime.UtcNow.Ticks - 5000
            }
        };

        var mockRepo = CreateMockRepository(queueItems);
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice(quantity1: 5, amount1: 250);

        // Second valid partial refund
        var secondPartialRefundRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = -3, // Total is 5, matching original
                    Amount = -150, // Total is 250, matching original
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)PTVATRates.NormalCase | (long)ChargeItemCaseFlags.Refund)
                }
            ]
        };

        // Act
        var result = await validator.ValidatePartialRefundAsync(secondPartialRefundRequest, originalRequest, "INV-001");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidatePartialRefund_WithNoRefundItems_ShouldPass()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var validator = CreateValidator(mockRepo.Object);

        var originalRequest = CreateOriginalInvoice();
        var normalRequest = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbPreviousReceiptReference = "INV-001",
            cbChargeItems =
            [
                new ChargeItem
                {
                    ProductNumber = "PROD-001",
                    Description = "Product 1",
                    Quantity = 1,
                    Amount = 50,
                    VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase // No refund flag
                }
            ]
        };

        // Act
        var result = await validator.ValidatePartialRefundAsync(normalRequest, originalRequest, "INV-001");

        // Assert
        result.Should().BeNull(); // Should pass as it's not a partial refund
    }

    #endregion
}

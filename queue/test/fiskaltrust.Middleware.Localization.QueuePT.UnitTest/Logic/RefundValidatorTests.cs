//using fiskaltrust.ifPOS.v2;
//using fiskaltrust.ifPOS.v2.Cases;
//using fiskaltrust.Middleware.Contracts.Repositories;
//using fiskaltrust.Middleware.Localization.QueuePT.Logic;
//using fiskaltrust.Middleware.Localization.QueuePT.Models;
//using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
//using fiskaltrust.Middleware.Localization.v2;
//using fiskaltrust.storage.V0;
//using FluentAssertions;
//using System.Text.Json;
//using Xunit;
//using Moq;
//using fiskaltrust.Middleware.Localization.v2.Helpers;

//namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Logic;

//public class RefundValidatorTests
//{
//    private RefundValidator CreateValidator(IMiddlewareQueueItemRepository repository)
//    {
//        return new RefundValidator(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository)));
//    }

//    private Mock<IMiddlewareQueueItemRepository> CreateMockRepository(List<ftQueueItem>? queueItems = null)
//    {
//        var mock = new Mock<IMiddlewareQueueItemRepository>();
//        var items = queueItems ?? new List<ftQueueItem>();

//        mock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
//            .Returns(items.ToAsyncEnumerable());

//        return mock;
//    }

//    private static ReceiptRequest CreateOriginalInvoice(decimal quantity1 = 2, decimal amount1 = 100, decimal quantity2 = 1, decimal amount2 = 50)
//    {
//        return new ReceiptRequest
//        {
//            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
//            cbReceiptReference = "INV-001",
//            cbTerminalID = "TERM-001",
//            cbReceiptMoment = DateTime.UtcNow,
//            cbChargeItems =
//            [
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-001",
//                    Description = "Product 1",
//                    Quantity = quantity1,
//                    Amount = amount1,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                },
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-002",
//                    Description = "Product 2",
//                    Quantity = quantity2,
//                    Amount = amount2,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                }
//            ]
//        };
//    }

  
//    [Fact]
//    public async Task ValidateFullRefund_WithMissingItem_ShouldReturnError()
//    {
//        // Arrange
//        var mockRepo = CreateMockRepository();
//        var validator = CreateValidator(mockRepo.Object);

//        var originalRequest = CreateOriginalInvoice();
//        var refundRequest = new ReceiptRequest
//        {
//            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
//            cbPreviousReceiptReference = "INV-001",
//            cbChargeItems =
//            [
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-001",
//                    Description = "Product 1",
//                    Quantity = -2,
//                    Amount = -100,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                }
//                // Missing PROD-002
//            ]
//        };

//        // Act
//        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

//        // Assert
//        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001"));
//    }

//    [Fact]
//    public async Task ValidateFullRefund_WithExtraItem_ShouldReturnError()
//    {
//        // Arrange
//        var mockRepo = CreateMockRepository();
//        var validator = CreateValidator(mockRepo.Object);

//        var originalRequest = CreateOriginalInvoice();
//        var refundRequest = new ReceiptRequest
//        {
//            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
//            cbPreviousReceiptReference = "INV-001",
//            cbChargeItems =
//            [
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-001",
//                    Description = "Product 1",
//                    Quantity = -2,
//                    Amount = -100,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                },
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-002",
//                    Description = "Product 2",
//                    Quantity = -1,
//                    Amount = -50,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                },
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-003",
//                    Description = "Extra Product",
//                    Quantity = -1,
//                    Amount = -25,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                }
//            ]
//        };

//        // Act
//        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

//        // Assert
//        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001", "Mismatch ChargeItems"));
//    }

//    [Fact]
//    public async Task ValidateFullRefund_WithIncorrectQuantity_ShouldReturnError()
//    {
//        // Arrange
//        var mockRepo = CreateMockRepository();
//        var validator = CreateValidator(mockRepo.Object);

//        var originalRequest = CreateOriginalInvoice();
//        var refundRequest = new ReceiptRequest
//        {
//            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
//            cbPreviousReceiptReference = "INV-001",
//            cbChargeItems =
//            [
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-001",
//                    Description = "Product 1",
//                    Quantity = -3, // Wrong quantity (should be -2)
//                    Amount = -100,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                },
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-002",
//                    Description = "Product 2",
//                    Quantity = -1,
//                    Amount = -50,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                }
//            ]
//        };

//        // Act
//        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

//        // Assert
//        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001"));
//    }

//    [Fact]
//    public async Task ValidateFullRefund_WithIncorrectAmount_ShouldReturnError()
//    {
//        // Arrange
//        var mockRepo = CreateMockRepository();
//        var validator = CreateValidator(mockRepo.Object);

//        var originalRequest = CreateOriginalInvoice();
//        var refundRequest = new ReceiptRequest
//        {
//            ftReceiptCase = (ReceiptCase) ((long) ReceiptCase.InvoiceB2C0x1001 | (long) ReceiptCaseFlags.Refund),
//            cbPreviousReceiptReference = "INV-001",
//            cbChargeItems =
//            [
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-001",
//                    Description = "Product 1",
//                    Quantity = -2,
//                    Amount = -90, // Wrong amount (should be -100)
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                },
//                new ChargeItem
//                {
//                    ProductNumber = "PROD-002",
//                    Description = "Product 2",
//                    Quantity = -1,
//                    Amount = -50,
//                    VATRate = 23m,
//                    ftChargeItemCase = (ChargeItemCase)PTVATRates.NormalCase
//                }
//            ]
//        };

//        // Act
//        var result = await validator.ValidateFullRefundAsync(refundRequest, originalRequest, "INV-001");

//        // Assert
//        result.Should().Be(ErrorMessagesPT.EEEE_FullRefundItemsMismatch("INV-001"));
//    }
//}

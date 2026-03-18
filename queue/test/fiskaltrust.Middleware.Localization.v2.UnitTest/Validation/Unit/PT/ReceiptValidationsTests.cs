using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models.Cases.PT;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using System.Text.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.PT;

public class ReceiptValidationsTests
{
    private static ReceiptReferenceProvider CreateProvider(
        bool hasExistingRefund = false,
        bool hasExistingPaymentTransfer = false,
        ReceiptRequest? originalReceipt = null)
    {
        var mockRepo = new Mock<IMiddlewareQueueItemRepository>();

        var queueItems = new List<ftQueueItem>();
        bool hasAnyItems = hasExistingRefund || hasExistingPaymentTransfer || originalReceipt != null;

        mockRepo.Setup(x => x.GetLastQueueItemAsync())
            .ReturnsAsync(hasAnyItems
                ? new ftQueueItem { ftQueueItemId = Guid.NewGuid() }
                : (ftQueueItem?)null);

        if (hasExistingRefund)
        {
            var refundRequest = new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
                cbPreviousReceiptReference = "ORIG-001"
            };
            var refundResponse = new ReceiptResponse { ftState = State.Success };
            queueItems.Add(new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftDoneMoment = DateTime.UtcNow,
                request = JsonSerializer.Serialize(refundRequest),
                response = JsonSerializer.Serialize(refundResponse),
                responseHash = "hash"
            });
        }

        if (hasExistingPaymentTransfer)
        {
            var transferRequest = new ReceiptRequest
            {
                ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
                cbPreviousReceiptReference = "ORIG-001"
            };
            var transferResponse = new ReceiptResponse { ftState = State.Success };
            queueItems.Add(new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftDoneMoment = DateTime.UtcNow,
                request = JsonSerializer.Serialize(transferRequest),
                response = JsonSerializer.Serialize(transferResponse),
                responseHash = "hash"
            });
        }

        if (originalReceipt != null)
        {
            var origResponse = new ReceiptResponse { ftState = State.Success };
            var origQueueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftDoneMoment = DateTime.UtcNow,
                cbReceiptReference = originalReceipt.cbReceiptReference,
                request = JsonSerializer.Serialize(originalReceipt),
                response = JsonSerializer.Serialize(origResponse),
                responseHash = "hash"
            };
            queueItems.Add(origQueueItem);

            mockRepo.Setup(x => x.GetByReceiptReferenceAsync(originalReceipt.cbReceiptReference, null))
                .Returns(new List<ftQueueItem> { origQueueItem }.ToAsyncEnumerable());
        }
        else
        {
            mockRepo.Setup(x => x.GetByReceiptReferenceAsync(It.IsAny<string>(), null))
                .Returns(AsyncEnumerable.Empty<ftQueueItem>());
        }

        mockRepo.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(queueItems.ToAsyncEnumerable());

        return new ReceiptReferenceProvider(
            new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(mockRepo.Object)));
    }

    #region PartialRefundMustNotContainNonRefundItems

    [Fact]
    public void PartialRefund_AllRefundItems_ShouldPass()
    {
        var validator = new ReceiptValidations.PartialRefundMustNotContainNonRefundItems();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = -50m, Quantity = -1,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.Refund)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PartialRefund_MixedItems_ShouldFail()
    {
        var validator = new ReceiptValidations.PartialRefundMustNotContainNonRefundItems();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = -50m, Quantity = -1,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.Refund)
                },
                new ChargeItem
                {
                    Amount = 30m, Quantity = 1,
                    ftChargeItemCase = ChargeItemCase.NormalVatRate // NOT a refund item
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("PartialRefundMixedItems");
    }

    #endregion

    #region HandwrittenMustNotBeRefundOrVoid

    [Fact]
    public void Handwritten_NoRefundNoVoid_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenMustNotBeRefundOrVoid();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.HandWritten),
            cbChargeItems = new List<ChargeItem>()
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Handwritten_WithRefund_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenMustNotBeRefundOrVoid();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001
                | (long)ReceiptCaseFlags.HandWritten
                | (long)ReceiptCaseFlags.Refund)
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("HandwrittenWithRefundOrVoid");
    }

    [Fact]
    public void Handwritten_WithVoid_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenMustNotBeRefundOrVoid();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001
                | (long)ReceiptCaseFlags.HandWritten
                | (long)ReceiptCaseFlags.Void)
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("HandwrittenWithRefundOrVoid");
    }

    [Fact]
    public void NonHandwritten_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenMustNotBeRefundOrVoid();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund)
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region HandwrittenMustHaveSeriesAndNumber

    [Fact]
    public void Handwritten_WithSeriesAndNumber_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenMustHaveSeriesAndNumber();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new ftReceiptCaseDataPayloadPT
            {
                PT = new ftReceiptCaseDataPortugalPayload { Series = "A", Number = 1 }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Handwritten_MissingSeries_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenMustHaveSeriesAndNumber();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new ftReceiptCaseDataPayloadPT
            {
                PT = new ftReceiptCaseDataPortugalPayload { Series = null, Number = 1 }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("HandwrittenMissingSeriesOrNumber");
    }

    [Fact]
    public void Handwritten_NullReceiptCaseData_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenMustHaveSeriesAndNumber();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("HandwrittenMissingSeriesOrNumber");
    }

    [Fact]
    public void NonHandwritten_WithoutSeriesAndNumber_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenMustHaveSeriesAndNumber();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            ftReceiptCaseData = null
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PaymentTransferMustHaveAccountReceivableItem

    [Fact]
    public void PaymentTransfer_WithReceivableItem_ShouldPass()
    {
        var validator = new ReceiptValidations.PaymentTransferMustHaveAccountReceivableItem();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Amount = 100m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseTypeOfService.Receivable)
                }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PaymentTransfer_WithoutReceivableItem_ShouldFail()
    {
        var validator = new ReceiptValidations.PaymentTransferMustHaveAccountReceivableItem();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 100m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("PaymentTransferMissingReceivableItem");
    }

    #endregion

    #region ReceiptMomentMustBeUtc

    [Fact]
    public void ReceiptMoment_Utc_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustBeUtc();
        var request = new ReceiptRequest { cbReceiptMoment = DateTime.UtcNow };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceiptMoment_Local_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustBeUtc();
        var request = new ReceiptRequest { cbReceiptMoment = DateTime.Now };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment).WithErrorCode("ReceiptMomentNotUtc");
    }

    #endregion

    #region ReceiptMomentMustNotBeInFuture

    [Fact]
    public void ReceiptMoment_Past_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustNotBeInFuture();
        var request = new ReceiptRequest { cbReceiptMoment = DateTime.UtcNow.AddMinutes(-1) };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceiptMoment_Future_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustNotBeInFuture();
        var request = new ReceiptRequest { cbReceiptMoment = DateTime.UtcNow.AddHours(1) };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment).WithErrorCode("ReceiptMomentInFuture");
    }

    #endregion

    #region ReceiptMomentDeviationLimit

    [Fact]
    public void ReceiptMoment_WithinDeviation_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptMomentDeviationLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptMoment = DateTime.UtcNow.AddMinutes(-5)
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceiptMoment_ExceedsDeviation_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptMomentDeviationLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptMoment = DateTime.UtcNow.AddMinutes(-15)
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment).WithErrorCode("ReceiptMomentDeviationExceeded");
    }

    [Fact]
    public void ReceiptMoment_Handwritten_SkipsDeviationCheck()
    {
        var validator = new ReceiptValidations.ReceiptMomentDeviationLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.HandWritten),
            cbReceiptMoment = DateTime.UtcNow.AddMinutes(-30)
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ReceiptMomentTimeDifference

    [Fact]
    public void ReceiptMoment_WithinOneMinute_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptMomentTimeDifference();
        var request = new ReceiptRequest { cbReceiptMoment = DateTime.UtcNow };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceiptMoment_OverOneMinute_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptMomentTimeDifference();
        var request = new ReceiptRequest { cbReceiptMoment = DateTime.UtcNow.AddMinutes(-2) };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment)
              .WithErrorCode("EEEE_ReceiptMomentTimeDifferenceExceeded");
    }

    #endregion

    #region RefundMustNotAlreadyExist

    [Fact]
    public async Task RefundNotExists_ShouldPass()
    {
        var provider = CreateProvider(hasExistingRefund: false);
        var validator = new ReceiptValidations.RefundMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RefundAlreadyExists_ShouldFail()
    {
        var provider = CreateProvider(hasExistingRefund: true);
        var validator = new ReceiptValidations.RefundMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "RefundAlreadyExists");
    }

    #endregion

    #region PaymentTransferMustNotAlreadyExist

    [Fact]
    public async Task PaymentTransferNotExists_ShouldPass()
    {
        var provider = CreateProvider(hasExistingPaymentTransfer: false);
        var validator = new ReceiptValidations.PaymentTransferMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PaymentTransferAlreadyExists_ShouldFail()
    {
        var provider = CreateProvider(hasExistingPaymentTransfer: true);
        var validator = new ReceiptValidations.PaymentTransferMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PaymentTransferAlreadyExists");
    }

    #endregion

    #region PaymentTransferOriginalMustBeInvoice

    [Fact]
    public async Task PaymentTransfer_OriginalIsInvoice_ShouldPass()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Amount = 100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 100m } }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.PaymentTransferOriginalMustBeInvoice(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "ORIG-001"
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PaymentTransfer_OriginalNotInvoice_ShouldFail()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Amount = 100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 100m } }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.PaymentTransferOriginalMustBeInvoice(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "ORIG-001"
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PaymentTransferOriginalNotInvoice");
    }

    [Fact]
    public async Task PaymentTransfer_OriginalNotFound_ShouldFail()
    {
        var provider = CreateProvider();
        var validator = new ReceiptValidations.PaymentTransferOriginalMustBeInvoice(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "NONEXISTENT"
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PaymentTransferOriginalNotInvoice");
    }

    #endregion

    #region PaymentTransferAmountsMustMatch

    [Fact]
    public async Task PaymentTransfer_AmountsMatch_ShouldPass()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Amount = 100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 100m, ftPayItemCase = PayItemCase.AccountsReceivable }
            }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.PaymentTransferAmountsMustMatch(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "ORIG-001",
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 100m }
            }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PaymentTransfer_AmountsDontMatch_ShouldFail()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Amount = 100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 100m, ftPayItemCase = PayItemCase.AccountsReceivable }
            }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.PaymentTransferAmountsMustMatch(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "ORIG-001",
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 75m }
            }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PaymentTransferAmountsMismatch");
    }

    [Fact]
    public async Task PaymentTransfer_OriginalNotFound_AmountsMismatch_ShouldFail()
    {
        var provider = CreateProvider();
        var validator = new ReceiptValidations.PaymentTransferAmountsMustMatch(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "NONEXISTENT",
            cbPayItems = new List<PayItem>
            {
                new PayItem { Amount = 100m }
            }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PaymentTransferAmountsMismatch");
    }

    #endregion
}

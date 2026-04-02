using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;
using fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class PTReceiptValidationsTests
{
    // ─── Shared helpers ────────────────────────────────────────────────────────

    private static FVReceiptReferenceProvider CreateProvider(Mock<IMiddlewareQueueItemRepository> repo)
        => new(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repo.Object)));

    private static DocumentStatusProvider CreateDocumentStatusProvider(Mock<IMiddlewareQueueItemRepository> repo)
        => new(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repo.Object)));

    private static VoidValidator CreateVoidValidator(Mock<IMiddlewareQueueItemRepository> repo)
        => new(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repo.Object)));

    private static RefundValidator CreateRefundValidator(Mock<IMiddlewareQueueItemRepository> repo)
        => new(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repo.Object)));

    private static ftQueueItem CreateFinishedItem(ReceiptRequest req, ReceiptResponse res) => new()
    {
        ftQueueItemId = Guid.NewGuid(),
        request = System.Text.Json.JsonSerializer.Serialize(req),
        response = System.Text.Json.JsonSerializer.Serialize(res),
        responseHash = "hash",
        ftDoneMoment = DateTime.UtcNow
    };

    private static ReceiptResponse SuccessResponse() => new();

    private static async IAsyncEnumerable<ftQueueItem> ToAsync(params ftQueueItem[] items)
    {
        foreach (var item in items) yield return item;
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ftQueueItem> EmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    // PT receipt cases
    private static ReceiptCase PtPosCase => ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT");
    private static ReceiptCase PtInvoiceB2CCase => ReceiptCase.InvoiceB2C0x1001.WithCountry("PT");
    private static ReceiptCase PtPaymentTransferCase => ReceiptCase.PaymentTransfer0x0002.WithCountry("PT");

    // ftReceiptCaseData must be an object (not a string) because TryDeserializeftReceiptCaseData
    // calls JsonSerializer.Serialize(ftReceiptCaseData) first — a string would be double-serialized.
    private static ftReceiptCaseDataPayloadPT HandWrittenCaseData(string series = "SERIES-A", long number = 1) =>
        new() { PT = new ftReceiptCaseDataPortugalPayload { Series = series, Number = number } };

    // ─── TrainingModeNotSupported ──────────────────────────────────────────────

    [Fact]
    public void TrainingModeNotSupported_TrainingFlag_ShouldFail()
    {
        var validator = new ReceiptValidations.TrainingModeNotSupported();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Training),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("TrainingModeNotSupported");
    }

    [Fact]
    public void TrainingModeNotSupported_NoTrainingFlag_ShouldPass()
    {
        var validator = new ReceiptValidations.TrainingModeNotSupported();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── RefundMustHavePreviousReference ──────────────────────────────────────

    [Fact]
    public void RefundMustHavePreviousReference_MissingReference_ShouldFail()
    {
        var validator = new ReceiptValidations.RefundMustHavePreviousReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("RefundMissingPreviousReceiptReference");
    }

    [Fact]
    public void RefundMustHavePreviousReference_WithReference_ShouldPass()
    {
        var validator = new ReceiptValidations.RefundMustHavePreviousReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "original-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PartialRefundMustNotContainNonRefundItems ─────────────────────────────

    [Fact]
    public void PartialRefundMustNotContainNonRefundItems_MixedItems_ShouldFail()
    {
        var validator = new ReceiptValidations.PartialRefundMustNotContainNonRefundItems();
        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var normalItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Refund item", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundItemCase },
                new ChargeItem { Description = "Normal item", VATRate = 23m, Amount = 10m, Quantity = 1m, ftChargeItemCase = normalItemCase }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbChargeItems)
            .WithErrorCode("PartialRefundMixedItems");
    }

    [Fact]
    public void PartialRefundMustNotContainNonRefundItems_AllRefundItems_ShouldPass()
    {
        var validator = new ReceiptValidations.PartialRefundMustNotContainNonRefundItems();
        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Refund item 1", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundItemCase },
                new ChargeItem { Description = "Refund item 2", VATRate = 23m, Amount = -3m, Quantity = -1m, ftChargeItemCase = refundItemCase }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── MixedRefundPayItemsNotAllowed ────────────────────────────────────────

    [Fact]
    public void MixedRefundPayItemsNotAllowed_MixedPayItems_ShouldFail()
    {
        var validator = new ReceiptValidations.MixedRefundPayItemsNotAllowed();
        var refundChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var refundPayItemCase = PayItemCase.CashPayment.WithCountry("PT").WithFlag(PayItemCaseFlags.Refund);
        var normalPayItemCase = PayItemCase.CashPayment.WithCountry("PT");
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Refund", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundChargeItemCase }
            ],
            cbPayItems =
            [
                new PayItem { Amount = -5m, ftPayItemCase = refundPayItemCase },
                new PayItem { Amount = 10m, ftPayItemCase = normalPayItemCase }
            ]
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPayItems)
            .WithErrorCode("PartialRefundMixedPayItems");
    }

    [Fact]
    public void MixedRefundPayItemsNotAllowed_AllRefundPayItems_ShouldPass()
    {
        var validator = new ReceiptValidations.MixedRefundPayItemsNotAllowed();
        var refundChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var refundPayItemCase = PayItemCase.CashPayment.WithCountry("PT").WithFlag(PayItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Refund", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundChargeItemCase }
            ],
            cbPayItems =
            [
                new PayItem { Amount = -5m, ftPayItemCase = refundPayItemCase }
            ]
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── HandwrittenMustNotBeRefundOrVoid ─────────────────────────────────────

    [Fact]
    public void HandwrittenMustNotBeRefundOrVoid_HandWrittenWithRefund_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenMustNotBeRefundOrVoid();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten).WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("HandwrittenWithRefundOrVoid");
    }

    [Fact]
    public void HandwrittenMustNotBeRefundOrVoid_HandWrittenNoRefundVoid_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenMustNotBeRefundOrVoid();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = HandWrittenCaseData(),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── HandwrittenMustHaveSeriesAndNumber ───────────────────────────────────

    [Fact]
    public void HandwrittenMustHaveSeriesAndNumber_MissingCaseData_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenMustHaveSeriesAndNumber();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = null,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("HandwrittenMissingSeriesOrNumber");
    }

    [Fact]
    public void HandwrittenMustHaveSeriesAndNumber_WithSeriesAndNumber_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenMustHaveSeriesAndNumber();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = HandWrittenCaseData("SERIES-A", 1),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── HandwrittenReceiptOnlyForInvoices ────────────────────────────────────

    [Fact]
    public void HandwrittenReceiptOnlyForInvoices_PosReceiptCase_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenReceiptOnlyForInvoices();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ftReceiptCase)
            .WithErrorCode("HandwrittenReceiptOnlyForInvoices");
    }

    [Fact]
    public void HandwrittenReceiptOnlyForInvoices_InvoiceReceiptCase_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenReceiptOnlyForInvoices();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── HandwrittenSeriesInvalidCharacter ────────────────────────────────────

    [Fact]
    public void HandwrittenSeriesInvalidCharacter_SeriesWithSpace_ShouldFail()
    {
        var validator = new ReceiptValidations.HandwrittenSeriesInvalidCharacter();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = HandWrittenCaseData("SERIES A", 1),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("HandwrittenSeriesInvalidCharacter");
    }

    [Fact]
    public void HandwrittenSeriesInvalidCharacter_SeriesWithoutSpace_ShouldPass()
    {
        var validator = new ReceiptValidations.HandwrittenSeriesInvalidCharacter();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = HandWrittenCaseData("SERIES-A", 1),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PaymentTransferMustHaveAccountReceivableItem ─────────────────────────

    [Fact]
    public void PaymentTransferMustHaveAccountReceivableItem_NoReceivableItem_ShouldFail()
    {
        var validator = new ReceiptValidations.PaymentTransferMustHaveAccountReceivableItem();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Item",
                VATRate = 23m,
                Amount = 10m,
                Quantity = 1m,
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery)
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbChargeItems)
            .WithErrorCode("PaymentTransferMissingReceivableItem");
    }

    [Fact]
    public void PaymentTransferMustHaveAccountReceivableItem_HasReceivableItem_ShouldPass()
    {
        var validator = new ReceiptValidations.PaymentTransferMustHaveAccountReceivableItem();
        var receivableCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Receivable);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbChargeItems = [new ChargeItem
            {
                Description = "Receivable",
                VATRate = 23m,
                Amount = 100m,
                Quantity = 1m,
                ftChargeItemCase = receivableCase
            }],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PaymentTransferOnlyOneReference ─────────────────────────────────────

    [Fact]
    public void PaymentTransferOnlyOneReference_MultipleRefs_ShouldFail()
    {
        var validator = new ReceiptValidations.PaymentTransferOnlyOneReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = new string[] { "ref-1", "ref-2" },
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("PaymentTransferMultipleReferences");
    }

    [Fact]
    public void PaymentTransferOnlyOneReference_SingleRef_ShouldPass()
    {
        var validator = new ReceiptValidations.PaymentTransferOnlyOneReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "ref-1",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PositionsMustBeSequential ────────────────────────────────────────────

    [Fact]
    public void PositionsMustBeSequential_GapInPositions_ShouldFail()
    {
        var validator = new ReceiptValidations.PositionsMustBeSequential();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbChargeItems =
            [
                new ChargeItem { Description = "Item 1", VATRate = 23m, Amount = 10m, Quantity = 1m, Position = 1 },
                new ChargeItem { Description = "Item 2", VATRate = 23m, Amount = 10m, Quantity = 1m, Position = 3 }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbChargeItems)
            .WithErrorCode("InvalidChargeItemPositions");
    }

    [Fact]
    public void PositionsMustBeSequential_SequentialPositions_ShouldPass()
    {
        var validator = new ReceiptValidations.PositionsMustBeSequential();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbChargeItems =
            [
                new ChargeItem { Description = "Item 1", VATRate = 23m, Amount = 10m, Quantity = 1m, Position = 1 },
                new ChargeItem { Description = "Item 2", VATRate = 23m, Amount = 10m, Quantity = 1m, Position = 2 },
                new ChargeItem { Description = "Item 3", VATRate = 23m, Amount = 10m, Quantity = 1m, Position = 3 }
            ],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── ReceiptMomentMustBeUtc ───────────────────────────────────────────────

    [Fact]
    public void ReceiptMomentMustBeUtc_LocalTime_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustBeUtc();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = DateTime.Now,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment)
            .WithErrorCode("ReceiptMomentNotUtc");
    }

    [Fact]
    public void ReceiptMomentMustBeUtc_UtcTime_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustBeUtc();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── ReceiptMomentMustNotBeInFuture ──────────────────────────────────────

    [Fact]
    public void ReceiptMomentMustNotBeInFuture_FutureTime_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustNotBeInFuture();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = DateTime.UtcNow.AddHours(1),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment)
            .WithErrorCode("ReceiptMomentInFuture");
    }

    [Fact]
    public void ReceiptMomentMustNotBeInFuture_PastTime_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptMomentMustNotBeInFuture();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = DateTime.UtcNow.AddMinutes(-1),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── ReceiptMomentDeviationLimit ─────────────────────────────────────────

    [Fact]
    public void ReceiptMomentDeviationLimit_ExceedsDeviationLimit_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptMomentDeviationLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = DateTime.UtcNow.AddMinutes(-11), // > 10 minutes in the past
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment)
            .WithErrorCode("ReceiptMomentDeviationExceeded");
    }

    [Fact]
    public void ReceiptMomentDeviationLimit_WithinDeviationLimit_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptMomentDeviationLimit();
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = DateTime.UtcNow.AddMinutes(-5),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── ReceiptReferenceAlreadyUsed (async) ─────────────────────────────────

    [Fact]
    public async Task ReceiptReferenceAlreadyUsed_RefAlreadyExists_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        var existingReq = new ReceiptRequest { cbReceiptReference = "dup-ref", ftReceiptCase = PtPosCase };
        var existingRes = SuccessResponse();
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingReq, existingRes)));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.ReceiptReferenceAlreadyUsed(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptReference = "dup-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptReference)
            .WithErrorCode("ReceiptReferenceAlreadyUsed");
    }

    [Fact]
    public async Task ReceiptReferenceAlreadyUsed_NewRef_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.ReceiptReferenceAlreadyUsed(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptReference = "new-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── RefundMustNotAlreadyExist (async) ───────────────────────────────────

    [Fact]
    public async Task RefundMustNotAlreadyExist_RefundAlreadyExists_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());

        var existingRefundReq = new ReceiptRequest
        {
            cbReceiptReference = "refund-1",
            cbPreviousReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund)
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingRefundReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.RefundMustNotAlreadyExist(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("RefundAlreadyExists");
    }

    [Fact]
    public async Task RefundMustNotAlreadyExist_NoExistingRefund_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem?) null);

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.RefundMustNotAlreadyExist(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── HandwrittenSeriesNumberAlreadyLinked (async) ─────────────────────────

    [Fact]
    public async Task HandwrittenSeriesNumberAlreadyLinked_AlreadyLinked_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var existingReq = new ReceiptRequest
        {
            cbReceiptReference = "hw-ref-1",
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = HandWrittenCaseData("SERIES-A", 1)
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.HandwrittenSeriesNumberAlreadyLinked(provider);

        var request = new ReceiptRequest
        {
            cbReceiptReference = "hw-ref-2",
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = HandWrittenCaseData("SERIES-A", 1),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("HandwrittenSeriesNumberAlreadyLinked");
    }

    [Fact]
    public async Task HandwrittenSeriesNumberAlreadyLinked_NotLinked_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.HandwrittenSeriesNumberAlreadyLinked(provider);

        var request = new ReceiptRequest
        {
            cbReceiptReference = "hw-ref-new",
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = HandWrittenCaseData("SERIES-B", 5),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PaymentTransferMustNotAlreadyExist (async) ──────────────────────────

    [Fact]
    public async Task PaymentTransferMustNotAlreadyExist_AlreadyExists_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());

        var existingTransferReq = new ReceiptRequest
        {
            cbReceiptReference = "pt-1",
            cbPreviousReceiptReference = "invoice-ref",
            ftReceiptCase = PtPaymentTransferCase
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingTransferReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferMustNotAlreadyExist(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("PaymentTransferAlreadyExists");
    }

    [Fact]
    public async Task PaymentTransferMustNotAlreadyExist_NoExistingTransfer_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem?) null);

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferMustNotAlreadyExist(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PaymentTransferOriginalMustBeInvoice (async) ────────────────────────

    [Fact]
    public async Task PaymentTransferOriginalMustBeInvoice_OriginalIsPosReceipt_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferOriginalMustBeInvoice(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("PaymentTransferOriginalNotInvoice");
    }

    [Fact]
    public async Task PaymentTransferOriginalMustBeInvoice_OriginalIsInvoice_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtInvoiceB2CCase
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferOriginalMustBeInvoice(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PaymentTransferAmountsMustMatch (async) ─────────────────────────────

    [Fact]
    public async Task PaymentTransferAmountsMustMatch_AmountExceedsReceivable_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var accountsReceivablePayItemCase = PayItemCase.AccountsReceivable.WithCountry("PT");
        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "invoice-ref",
            ftReceiptCase = PtInvoiceB2CCase,
            cbChargeItems = [],
            cbPayItems = [new PayItem { Amount = 100m, ftPayItemCase = accountsReceivablePayItemCase }]
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("invoice-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferAmountsMustMatch(provider);

        var request = new ReceiptRequest
        {
            cbReceiptReference = "transfer-1",
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbChargeItems = [],
            cbPayItems = [new PayItem { Amount = 150m }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("PaymentTransferAmountsMismatch");
    }

    [Fact]
    public async Task PaymentTransferAmountsMustMatch_AmountWithinReceivable_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var accountsReceivablePayItemCase = PayItemCase.AccountsReceivable.WithCountry("PT");
        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "invoice-ref",
            ftReceiptCase = PtInvoiceB2CCase,
            cbChargeItems = [],
            cbPayItems = [new PayItem { Amount = 100m, ftPayItemCase = accountsReceivablePayItemCase }]
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("invoice-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferAmountsMustMatch(provider);

        var request = new ReceiptRequest
        {
            cbReceiptReference = "transfer-1",
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbChargeItems = [],
            cbPayItems = [new PayItem { Amount = 80m }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PartialRefundPreviousReceiptMustNotBeVoided (async) ─────────────────

    [Fact]
    public async Task PartialRefundPreviousReceiptMustNotBeVoided_OriginalIsVoided_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());

        var voidReq = new ReceiptRequest
        {
            cbReceiptReference = "void-1",
            cbPreviousReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Void)
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(voidReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PartialRefundPreviousReceiptMustNotBeVoided(provider);

        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Refund", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundItemCase }
            ],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("PreviousReceiptIsVoided");
    }

    [Fact]
    public async Task PartialRefundPreviousReceiptMustNotBeVoided_OriginalNotVoided_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem?) null);

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PartialRefundPreviousReceiptMustNotBeVoided(provider);

        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Refund", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundItemCase }
            ],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PartialRefundMustNotHaveExistingRefund (async) ──────────────────────

    [Fact]
    public async Task PartialRefundMustNotHaveExistingRefund_FullRefundExists_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());

        var existingRefund = new ReceiptRequest
        {
            cbReceiptReference = "refund-1",
            cbPreviousReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund)
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingRefund, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PartialRefundMustNotHaveExistingRefund(provider);

        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Partial Refund", VATRate = 23m, Amount = -3m, Quantity = -1m, ftChargeItemCase = refundItemCase }
            ],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("RefundAlreadyExists");
    }

    [Fact]
    public async Task PartialRefundMustNotHaveExistingRefund_NoFullRefund_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem?) null);

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PartialRefundMustNotHaveExistingRefund(provider);

        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Partial Refund", VATRate = 23m, Amount = -3m, Quantity = -1m, ftChargeItemCase = refundItemCase }
            ],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PaymentTransferForRefundedReceipt (async) ────────────────────────────

    [Fact]
    public async Task PaymentTransferForRefundedReceipt_OriginalRefunded_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());

        var existingRefund = new ReceiptRequest
        {
            cbReceiptReference = "refund-1",
            cbPreviousReceiptReference = "invoice-ref",
            ftReceiptCase = PtInvoiceB2CCase.WithFlag(ReceiptCaseFlags.Refund)
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingRefund, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferForRefundedReceipt(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("PaymentTransferForRefundedReceipt");
    }

    [Fact]
    public async Task PaymentTransferForRefundedReceipt_OriginalNotRefunded_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem?) null);

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferForRefundedReceipt(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VoidDocumentStatusCheck (async) ─────────────────────────────────────

    [Fact]
    public async Task VoidDocumentStatusCheck_OriginalAlreadyVoided_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase,
            cbChargeItems = [],
            cbPayItems = []
        };

        var existingVoidReq = new ReceiptRequest
        {
            cbReceiptReference = "void-1",
            cbPreviousReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Void),
            cbChargeItems = [],
            cbPayItems = []
        };

        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));

        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingVoidReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var docStatusProvider = CreateDocumentStatusProvider(repo);
        var validator = new ReceiptValidations.VoidDocumentStatusCheck(provider, docStatusProvider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("VoidAlreadyExists");
    }

    [Fact]
    public async Task VoidDocumentStatusCheck_OriginalNotVoided_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase,
            cbChargeItems = [],
            cbPayItems = []
        };

        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));

        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var docStatusProvider = CreateDocumentStatusProvider(repo);
        var validator = new ReceiptValidations.VoidDocumentStatusCheck(provider, docStatusProvider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VoidFieldsMatch (async) ──────────────────────────────────────────────

    [Fact]
    public async Task VoidFieldsMatch_OriginalNotFound_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var voidValidator = CreateVoidValidator(repo);
        var validator = new ReceiptValidations.VoidFieldsMatch(provider, voidValidator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = 23m, Amount = -10m, Quantity = -1m }],
            cbPayItems = [new PayItem { Amount = -10m }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task VoidFieldsMatch_ItemCountMismatch_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase,
            Currency = ifPOS.v2.Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow.AddDays(-1),
            cbChargeItems =
            [
                new ChargeItem { Description = "Item 1", VATRate = 23m, Amount = 10m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) },
                new ChargeItem { Description = "Item 2", VATRate = 23m, Amount = 5m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }
            ],
            cbPayItems = [new PayItem { Amount = 15m, ftPayItemCase = PayItemCase.CashPayment.WithCountry("PT") }]
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var voidValidator = CreateVoidValidator(repo);
        var validator = new ReceiptValidations.VoidFieldsMatch(provider, voidValidator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "orig-ref",
            Currency = ifPOS.v2.Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem { Description = "Item 1", VATRate = 23m, Amount = -10m, Quantity = -1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }
            ],
            cbPayItems = [new PayItem { Amount = -10m, ftPayItemCase = PayItemCase.CashPayment.WithCountry("PT") }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("VoidItemsMismatch");
    }

    // ─── FullRefundFieldsMatch (async) ────────────────────────────────────────

    [Fact]
    public async Task FullRefundFieldsMatch_OriginalNotFound_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var refundValidator = CreateRefundValidator(repo);
        var validator = new ReceiptValidations.FullRefundFieldsMatch(provider, refundValidator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = 23m, Amount = -10m, Quantity = -1m }],
            cbPayItems = [new PayItem { Amount = -10m }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task FullRefundFieldsMatch_ItemCountMismatch_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase,
            Currency = ifPOS.v2.Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow.AddDays(-1),
            cbChargeItems =
            [
                new ChargeItem { Description = "Item 1", VATRate = 23m, Amount = 10m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) },
                new ChargeItem { Description = "Item 2", VATRate = 23m, Amount = 5m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }
            ],
            cbPayItems = [new PayItem { Amount = 15m, ftPayItemCase = PayItemCase.CashPayment.WithCountry("PT") }]
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var refundValidator = CreateRefundValidator(repo);
        var validator = new ReceiptValidations.FullRefundFieldsMatch(provider, refundValidator);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase.WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "orig-ref",
            Currency = ifPOS.v2.Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem { Description = "Item 1", VATRate = 23m, Amount = -10m, Quantity = -1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }
            ],
            cbPayItems = [new PayItem { Amount = -15m, ftPayItemCase = PayItemCase.CashPayment.WithCountry("PT") }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("FullRefundItemsMismatch");
    }

    // ─── PartialRefundFieldsMatch (async) ─────────────────────────────────────

    [Fact]
    public async Task PartialRefundFieldsMatch_OriginalNotFound_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var refundValidator = CreateRefundValidator(repo);
        var validator = new ReceiptValidations.PartialRefundFieldsMatch(provider, refundValidator);

        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Partial refund", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundItemCase }
            ],
            cbPayItems = [new PayItem { Amount = -5m }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task PartialRefundFieldsMatch_ItemNotFoundInOriginal_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase,
            Currency = ifPOS.v2.Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow.AddDays(-1),
            cbChargeItems =
            [
                new ChargeItem { Description = "Original Item", VATRate = 23m, Amount = 10m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }
            ],
            cbPayItems = [new PayItem { Amount = 10m, ftPayItemCase = PayItemCase.CashPayment.WithCountry("PT") }]
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var refundValidator = CreateRefundValidator(repo);
        var validator = new ReceiptValidations.PartialRefundFieldsMatch(provider, refundValidator);

        var refundItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund);
        var request = new ReceiptRequest
        {
            cbReceiptReference = "partial-refund-1",
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            Currency = ifPOS.v2.Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem { Description = "Different Item", VATRate = 23m, Amount = -5m, Quantity = -1m, ftChargeItemCase = refundItemCase }
            ],
            cbPayItems = [new PayItem { Amount = -5m, ftPayItemCase = PayItemCase.CashPayment.WithCountry("PT") }]
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("PartialRefundItemsMismatch");
    }

    // ─── PaymentTransferCustomerMismatch (async) ──────────────────────────────

    [Fact]
    public async Task PaymentTransferCustomerMismatch_OriginalNotFound_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetByReceiptReferenceAsync("invoice-ref", It.IsAny<string?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferCustomerMismatch(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbCustomer = System.Text.Json.JsonSerializer.Serialize(new MiddlewareCustomer { CustomerVATId = "501341600" }),
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task PaymentTransferCustomerMismatch_CustomerDataDiffers_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "invoice-ref",
            ftReceiptCase = PtInvoiceB2CCase,
            cbCustomer = new MiddlewareCustomer { CustomerVATId = "501341600", CustomerName = "Company A" },
            cbChargeItems = [],
            cbPayItems = []
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("invoice-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PaymentTransferCustomerMismatch(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPaymentTransferCase,
            cbPreviousReceiptReference = "invoice-ref",
            cbCustomer = new MiddlewareCustomer { CustomerVATId = "501341600", CustomerName = "Company B" },
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("PaymentTransferCustomerMismatch");
    }

    // ─── PreviousReceiptLineItemsMatch (async) ────────────────────────────────

    [Fact]
    public async Task PreviousReceiptLineItemsMatch_OriginalNotFound_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(EmptyAsync());

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PreviousReceiptLineItemsMatch(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems = [new ChargeItem { Description = "Item", VATRate = 23m, Amount = 10m, Quantity = 1m }],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task PreviousReceiptLineItemsMatch_NoMatchingItems_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();

        var originalReq = new ReceiptRequest
        {
            cbReceiptReference = "orig-ref",
            ftReceiptCase = PtPosCase,
            cbChargeItems =
            [
                new ChargeItem { Description = "Original Product", VATRate = 23m, Amount = 10m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }
            ],
            cbPayItems = []
        };
        repo.Setup(r => r.GetByReceiptReferenceAsync("orig-ref", It.IsAny<string?>()))
            .Returns(ToAsync(CreateFinishedItem(originalReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PreviousReceiptLineItemsMatch(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbPreviousReceiptReference = "orig-ref",
            cbChargeItems =
            [
                new ChargeItem { Description = "Different Product", VATRate = 23m, Amount = 10m, Quantity = 1m, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery) }
            ],
            cbPayItems = []
        };
        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("PreviousReceiptLineItemMismatch");
    }

    // ─── ReceiptMomentTimeDifference ─────────────────────────────────────────

    [Fact]
    public void ReceiptMomentTimeDifference_ExceedsDifferenceLimit_ShouldFail()
    {
        var serverMoment = DateTime.UtcNow;
        var response = new ReceiptResponse { ftReceiptMoment = serverMoment };
        var validator = new ReceiptValidations.ReceiptMomentTimeDifference(response);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = serverMoment.AddMinutes(-2), // 2 min difference > 1 min limit
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptMoment)
            .WithErrorCode("ReceiptMomentTimeDifferenceExceeded");
    }

    [Fact]
    public void ReceiptMomentTimeDifference_WithinDifferenceLimit_ShouldPass()
    {
        var serverMoment = DateTime.UtcNow;
        var response = new ReceiptResponse { ftReceiptMoment = serverMoment };
        var validator = new ReceiptValidations.ReceiptMomentTimeDifference(response);

        var request = new ReceiptRequest
        {
            ftReceiptCase = PtPosCase,
            cbReceiptMoment = serverMoment.AddSeconds(-30), // 30 sec difference < 1 min limit
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

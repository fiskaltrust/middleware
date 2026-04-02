using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using fiskaltrust.storage.V0;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

public class ReceiptValidationsTests
{
    // ─── Shared async helpers ──────────────────────────────────────────────────

    private static ReceiptReferenceProvider CreateProvider(Mock<IMiddlewareQueueItemRepository> repo)
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
        foreach (var item in items)
            yield return item;
        await Task.CompletedTask;
    }

    // ─── MandatoryCollections ──────────────────────────────────────────────────

    [Fact]
    public void MandatoryCollections_NullChargeItems_ShouldFail()
    {
        var validator = new ReceiptValidations.MandatoryCollections();
        var request = new ReceiptRequest
        {
            cbChargeItems = null,
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbChargeItems)
            .WithErrorCode("ChargeItemsMissing");
    }

    [Fact]
    public void MandatoryCollections_NullPayItems_ShouldFail()
    {
        var validator = new ReceiptValidations.MandatoryCollections();
        var request = new ReceiptRequest
        {
            cbChargeItems = [],
            cbPayItems = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPayItems)
            .WithErrorCode("PayItemsMissing");
    }

    [Fact]
    public void MandatoryCollections_BothSet_ShouldPass()
    {
        var validator = new ReceiptValidations.MandatoryCollections();
        var request = new ReceiptRequest
        {
            cbChargeItems = [],
            cbPayItems = []
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── CurrencyMustBeEur ─────────────────────────────────────────────────────

    [Fact]
    public void CurrencyMustBeEur_NonEurCurrency_ShouldFail()
    {
        var validator = new ReceiptValidations.CurrencyMustBeEur();
        var request = new ReceiptRequest
        {
            Currency = Currency.CHF
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorCode("OnlyEuroCurrencySupported");
    }

    [Fact]
    public void CurrencyMustBeEur_EurCurrency_ShouldPass()
    {
        var validator = new ReceiptValidations.CurrencyMustBeEur();
        var request = new ReceiptRequest
        {
            Currency = Currency.EUR
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── ReceiptBalance ────────────────────────────────────────────────────────

    [Fact]
    public void ReceiptBalance_Unbalanced_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptBalance();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem { Amount = 100m }],
            cbPayItems = [new PayItem { Amount = 50m }]
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("ReceiptNotBalanced");
    }

    [Fact]
    public void ReceiptBalance_Balanced_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptBalance();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem { Amount = 100m }],
            cbPayItems = [new PayItem { Amount = 100m }]
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceiptBalance_HandWritten_ShouldSkip()
    {
        var validator = new ReceiptValidations.ReceiptBalance();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbChargeItems = [new ChargeItem { Amount = 100m }],
            cbPayItems = [new PayItem { Amount = 50m }]
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── RefundReference ──────────────────────────────────────────────────────

    [Fact]
    public void RefundReference_RefundWithoutPreviousRef_ShouldFail()
    {
        var validator = new ReceiptValidations.RefundReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("RefundMissingPreviousReceiptReference");
    }

    [Fact]
    public void RefundReference_RefundWithPreviousRef_ShouldPass()
    {
        var validator = new ReceiptValidations.RefundReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ref-001"
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RefundReference_HandWritten_ShouldSkip()
    {
        var validator = new ReceiptValidations.RefundReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund).WithFlag(ReceiptCaseFlags.HandWritten),
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PaymentTransferReference ─────────────────────────────────────────────

    [Fact]
    public void PaymentTransferReference_PaymentTransferWithoutRef_ShouldFail()
    {
        var validator = new ReceiptValidations.PaymentTransferReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"),
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("PaymentTransferMissingPreviousReceiptReference");
    }

    [Fact]
    public void PaymentTransferReference_PaymentTransferWithRef_ShouldPass()
    {
        var validator = new ReceiptValidations.PaymentTransferReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002.WithCountry("PT"),
            cbPreviousReceiptReference = "ref-001"
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PaymentTransferReference_HandWritten_ShouldSkip()
    {
        var validator = new ReceiptValidations.PaymentTransferReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── RefundMustUseSingleReference ─────────────────────────────────────────

    [Fact]
    public void RefundMustUseSingleReference_GroupReference_ShouldFail()
    {
        var validator = new ReceiptValidations.RefundMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = new[] { "ref-001", "ref-002" }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("RefundGroupReferenceNotSupported");
    }

    [Fact]
    public void RefundMustUseSingleReference_SingleReference_ShouldPass()
    {
        var validator = new ReceiptValidations.RefundMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ref-001"
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RefundMustUseSingleReference_GrCountry_ShouldSkip()
    {
        var validator = new ReceiptValidations.RefundMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("GR").WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = new[] { "ref-001", "ref-002" }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PartialRefundMustUseSingleReference ──────────────────────────────────

    [Fact]
    public void PartialRefundMustUseSingleReference_GroupReference_ShouldFail()
    {
        var validator = new ReceiptValidations.PartialRefundMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund)
            }],
            cbPayItems = [],
            cbPreviousReceiptReference = new[] { "ref-001", "ref-002" }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("PartialRefundGroupReferenceNotSupported");
    }

    [Fact]
    public void PartialRefundMustUseSingleReference_SingleReference_ShouldPass()
    {
        var validator = new ReceiptValidations.PartialRefundMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [new ChargeItem
            {
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("PT").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund)
            }],
            cbPayItems = [],
            cbPreviousReceiptReference = "ref-001"
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PartialRefundMustUseSingleReference_GrCountry_ShouldSkip()
    {
        var validator = new ReceiptValidations.PartialRefundMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("GR"),
            cbChargeItems = [new ChargeItem
            {
                ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("GR").WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithFlag(ChargeItemCaseFlags.Refund)
            }],
            cbPayItems = [],
            cbPreviousReceiptReference = new[] { "ref-001", "ref-002" }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VoidMustUseSingleReference ───────────────────────────────────────────

    [Fact]
    public void VoidMustUseSingleReference_GroupReference_ShouldFail()
    {
        var validator = new ReceiptValidations.VoidMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = new[] { "ref-001", "ref-002" }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("VoidGroupReferenceNotSupported");
    }

    [Fact]
    public void VoidMustUseSingleReference_SingleReference_ShouldPass()
    {
        var validator = new ReceiptValidations.VoidMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "ref-001"
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void VoidMustUseSingleReference_GrCountry_ShouldSkip()
    {
        var validator = new ReceiptValidations.VoidMustUseSingleReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("GR").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = new[] { "ref-001", "ref-002" }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── CountryConsistency (receipt level) ───────────────────────────────────

    [Fact]
    public void CountryConsistency_ReceiptCountryMismatch_ShouldFail()
    {
        var queue = new ftQueue { CountryCode = "PT" };
        var validator = new ReceiptValidations.CountryConsistency(queue);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES")
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("ReceiptCaseCountryMismatch");
    }

    [Fact]
    public void CountryConsistency_MatchingCountry_ShouldPass()
    {
        var queue = new ftQueue { CountryCode = "PT" };
        var validator = new ReceiptValidations.CountryConsistency(queue);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT")
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CountryConsistency_NullQueue_ShouldSkip()
    {
        var validator = new ReceiptValidations.CountryConsistency(null);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES")
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PayItemCaseCountryConsistency ────────────────────────────────────────

    [Fact]
    public void PayItemCaseCountryConsistency_PayItemCountryMismatch_ShouldFail()
    {
        var queue = new ftQueue { CountryCode = "PT" };
        var validator = new ReceiptValidations.PayItemCaseCountryConsistency(queue);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [],
            cbPayItems = [new PayItem
            {
                ftPayItemCase = PayItemCase.CashPayment.WithCountry("ES")
            }]
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor("cbPayItems[0].ftPayItemCase")
            .WithErrorCode("PayItemCaseCountryMismatch");
    }

    [Fact]
    public void PayItemCaseCountryConsistency_MatchingCountry_ShouldPass()
    {
        var queue = new ftQueue { CountryCode = "PT" };
        var validator = new ReceiptValidations.PayItemCaseCountryConsistency(queue);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [],
            cbPayItems = [new PayItem
            {
                ftPayItemCase = PayItemCase.CashPayment.WithCountry("PT")
            }]
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PayItemCaseCountryConsistency_NullQueue_ShouldSkip()
    {
        var validator = new ReceiptValidations.PayItemCaseCountryConsistency(null);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT"),
            cbChargeItems = [],
            cbPayItems = [new PayItem
            {
                ftPayItemCase = PayItemCase.CashPayment.WithCountry("ES")
            }]
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── PreviousReceiptMustNotBeVoided ───────────────────────────────────────

    [Fact]
    public async Task PreviousReceiptMustNotBeVoided_ReferencedReceiptIsVoided_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());

        var voidReq = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "original-ref"
        };
        var voidItem = CreateFinishedItem(voidReq, SuccessResponse());

        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(voidItem));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PreviousReceiptMustNotBeVoided(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "original-ref"
        };

        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("PreviousReceiptIsVoided");
    }

    [Fact]
    public async Task PreviousReceiptMustNotBeVoided_ReferencedReceiptNotVoided_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem?) null);

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PreviousReceiptMustNotBeVoided(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "original-ref"
        };

        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task PreviousReceiptMustNotBeVoided_HandWritten_ShouldSkip()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());
        var voidReq = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "original-ref"
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(voidReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PreviousReceiptMustNotBeVoided(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.HandWritten),
            cbPreviousReceiptReference = "original-ref"
        };

        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task PreviousReceiptMustNotBeVoided_VoidCase_ShouldSkip()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.PreviousReceiptMustNotBeVoided(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "original-ref"
        };

        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── VoidMustNotAlreadyExist ──────────────────────────────────────────────

    [Fact]
    public async Task VoidMustNotAlreadyExist_VoidAlreadyExists_ShouldFail()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync(new ftQueueItem());

        var existingVoidReq = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "original-ref"
        };
        repo.Setup(r => r.GetEntriesOnOrAfterTimeStampAsync(0, It.IsAny<int?>()))
            .Returns(ToAsync(CreateFinishedItem(existingVoidReq, SuccessResponse())));

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.VoidMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "original-ref"
        };

        var result = await validator.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
            .WithErrorCode("VoidAlreadyExists");
    }

    [Fact]
    public async Task VoidMustNotAlreadyExist_NoExistingVoid_ShouldPass()
    {
        var repo = new Mock<IMiddlewareQueueItemRepository>();
        repo.Setup(r => r.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem?) null);

        var provider = CreateProvider(repo);
        var validator = new ReceiptValidations.VoidMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("PT").WithFlag(ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "original-ref"
        };

        var result = await validator.TestValidateAsync(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

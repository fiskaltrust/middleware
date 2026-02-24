using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using System.Text.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation.Unit.Global;

public class ReceiptValidationsTests
{
    private static ReceiptReferenceProvider CreateProvider(
        bool hasExistingVoid = false,
        bool hasExistingRefund = false,
        ReceiptRequest? originalReceipt = null,
        List<ReceiptRequest>? existingPartialRefunds = null)
    {
        var mockRepo = new Mock<IMiddlewareQueueItemRepository>();

        mockRepo.Setup(x => x.GetLastQueueItemAsync())
            .ReturnsAsync(hasExistingVoid || hasExistingRefund || originalReceipt != null || existingPartialRefunds?.Count > 0
                ? new ftQueueItem { ftQueueItemId = Guid.NewGuid() }
                : (ftQueueItem?)null);

        var queueItems = new List<ftQueueItem>();

        if (hasExistingVoid)
        {
            var voidRequest = new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Void),
                cbPreviousReceiptReference = "ORIG-001"
            };
            var voidResponse = new ReceiptResponse { ftState = State.Success };
            queueItems.Add(new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftDoneMoment = DateTime.UtcNow,
                request = JsonSerializer.Serialize(voidRequest),
                response = JsonSerializer.Serialize(voidResponse),
                responseHash = "hash"
            });
        }

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

        if (existingPartialRefunds != null)
        {
            foreach (var partialRefund in existingPartialRefunds)
            {
                queueItems.Add(new ftQueueItem
                {
                    ftQueueItemId = Guid.NewGuid(),
                    ftDoneMoment = DateTime.UtcNow,
                    request = JsonSerializer.Serialize(partialRefund),
                    response = JsonSerializer.Serialize(new ReceiptResponse { ftState = State.Success }),
                    responseHash = "hash"
                });
            }
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

    #region MandatoryCollections

    [Fact]
    public void MandatoryCollections_NullChargeItems_ShouldFail()
    {
        var validator = new ReceiptValidations.MandatoryCollections();
        var request = new ReceiptRequest { cbChargeItems = null, cbPayItems = new List<PayItem>() };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbChargeItems).WithErrorCode("ChargeItemsMissing");
    }

    [Fact]
    public void MandatoryCollections_NullPayItems_ShouldFail()
    {
        var validator = new ReceiptValidations.MandatoryCollections();
        var request = new ReceiptRequest { cbChargeItems = new List<ChargeItem>(), cbPayItems = null };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPayItems).WithErrorCode("PayItemsMissing");
    }

    [Fact]
    public void MandatoryCollections_BothPresent_ShouldPass()
    {
        var validator = new ReceiptValidations.MandatoryCollections();
        var request = new ReceiptRequest { cbChargeItems = new List<ChargeItem>(), cbPayItems = new List<PayItem>() };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CurrencyMustBeEur

    [Fact]
    public void Currency_EUR_ShouldPass()
    {
        var validator = new ReceiptValidations.CurrencyMustBeEur();
        var request = new ReceiptRequest { Currency = Currency.EUR };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Currency_NonEUR_ShouldFail()
    {
        var validator = new ReceiptValidations.CurrencyMustBeEur();
        var request = new ReceiptRequest { Currency = (Currency)840 }; // USD
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Currency).WithErrorCode("OnlyEuroCurrencySupported");
    }

    #endregion

    #region ChargeItemsAmountSum

    [Fact]
    public void ChargeItemsSum_MatchesReceiptAmount_ShouldPass()
    {
        var validator = new ReceiptValidations.ChargeItemsAmountSum();
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 25.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 10.00m },
                new ChargeItem { Amount = 15.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    [Fact]
    public void ChargeItemsSum_DoesNotMatch_ShouldFail()
    {
        var validator = new ReceiptValidations.ChargeItemsAmountSum();
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 100.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 10.00m },
                new ChargeItem { Amount = 15.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbReceiptAmount).WithErrorCode("ChargeItemsSumMismatch");
    }

    [Fact]
    public void ChargeItemsSum_NullReceiptAmount_ShouldPass()
    {
        var validator = new ReceiptValidations.ChargeItemsAmountSum();
        var request = new ReceiptRequest
        {
            cbReceiptAmount = null,
            cbChargeItems = new List<ChargeItem> { new ChargeItem { Amount = 10.00m } }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    [Fact]
    public void ChargeItemsSum_NullChargeItems_ShouldPass()
    {
        var validator = new ReceiptValidations.ChargeItemsAmountSum();
        var request = new ReceiptRequest { cbReceiptAmount = 10.00m, cbChargeItems = null };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    [Fact]
    public void ChargeItemsSum_WithNegativeAmounts_MatchesReceiptAmount_ShouldPass()
    {
        var validator = new ReceiptValidations.ChargeItemsAmountSum();
        var request = new ReceiptRequest
        {
            cbReceiptAmount = 8.00m,
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Amount = 10.00m },
                new ChargeItem { Amount = -2.00m }
            }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.cbReceiptAmount);
    }

    #endregion

    #region ReceiptBalance

    [Fact]
    public void ReceiptBalance_Balanced_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptBalance();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem> { new ChargeItem { Amount = 100m } },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 100m } }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceiptBalance_Unbalanced_ShouldFail()
    {
        var validator = new ReceiptValidations.ReceiptBalance();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem> { new ChargeItem { Amount = 100m } },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 50m } }
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveAnyValidationError().WithErrorCode("ReceiptNotBalanced");
    }

    [Fact]
    public void ReceiptBalance_WithinTolerance_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptBalance();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbChargeItems = new List<ChargeItem> { new ChargeItem { Amount = 100.00m } },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 100.01m } }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceiptBalance_SkippedForVoidCase_ShouldPass()
    {
        var validator = new ReceiptValidations.ReceiptBalance();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)0x0006,
            cbChargeItems = new List<ChargeItem> { new ChargeItem { Amount = 100m } },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 0m } }
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region RefundReference

    [Fact]
    public void RefundReference_RefundWithReference_ShouldPass()
    {
        var validator = new ReceiptValidations.RefundReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "REF-001"
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.cbPreviousReceiptReference);
    }

    [Fact]
    public void RefundReference_RefundWithoutReference_ShouldFail()
    {
        var validator = new ReceiptValidations.RefundReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
              .WithErrorCode("RefundMissingPreviousReceiptReference");
    }

    [Fact]
    public void RefundReference_NonRefundWithoutReference_ShouldPass()
    {
        var validator = new ReceiptValidations.RefundReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PaymentTransferReference

    [Fact]
    public void PaymentTransferReference_WithReference_ShouldPass()
    {
        var validator = new ReceiptValidations.PaymentTransferReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = "INV-001"
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PaymentTransferReference_WithoutReference_ShouldFail()
    {
        var validator = new ReceiptValidations.PaymentTransferReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PaymentTransfer0x0002,
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.cbPreviousReceiptReference)
              .WithErrorCode("PaymentTransferMissingPreviousReceiptReference");
    }

    [Fact]
    public void PaymentTransferReference_NonTransferWithoutReference_ShouldPass()
    {
        var validator = new ReceiptValidations.PaymentTransferReference();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbPreviousReceiptReference = null
        };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PreviousReceiptMustNotBeVoided

    [Fact]
    public async Task PreviousReceiptNotVoided_ShouldPass()
    {
        var provider = CreateProvider(hasExistingVoid: false);
        var validator = new ReceiptValidations.PreviousReceiptMustNotBeVoided(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PreviousReceiptAlreadyVoided_ShouldFail()
    {
        var provider = CreateProvider(hasExistingVoid: true);
        var validator = new ReceiptValidations.PreviousReceiptMustNotBeVoided(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PreviousReceiptIsVoided");
    }

    [Fact]
    public async Task PreviousReceiptNotVoided_VoidFlag_SkipsRule()
    {
        var provider = CreateProvider(hasExistingVoid: true);
        var validator = new ReceiptValidations.PreviousReceiptMustNotBeVoided(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region VoidMustNotAlreadyExist

    [Fact]
    public async Task VoidNotExists_ShouldPass()
    {
        var provider = CreateProvider(hasExistingVoid: false);
        var validator = new ReceiptValidations.VoidMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VoidAlreadyExists_ShouldFail()
    {
        var provider = CreateProvider(hasExistingVoid: true);
        var validator = new ReceiptValidations.VoidMustNotAlreadyExist(provider);
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "ORIG-001"
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "VoidAlreadyExists");
    }

    #endregion

    #region FullRefundMustMatchOriginal

    [Fact]
    public async Task FullRefund_MatchesOriginal_ShouldPass()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Quantity = 2, Amount = 100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Description = "Cash", Quantity = 1, Amount = 100m }
            }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.FullRefundMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Quantity = -2, Amount = -100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Description = "Cash", Quantity = -1, Amount = -100m }
            }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FullRefund_DifferentItemCount_ShouldFail()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product 1", Quantity = 1, Amount = 50m, VATRate = 23m },
                new ChargeItem { Description = "Product 2", Quantity = 1, Amount = 50m, VATRate = 23m }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 100m } }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.FullRefundMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product 1", Quantity = -1, Amount = -50m, VATRate = 23m }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = -100m } }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "FullRefundItemsMismatch");
    }

    [Fact]
    public async Task FullRefund_OriginalNotFound_ShouldFail()
    {
        var provider = CreateProvider(); // no original
        var validator = new ReceiptValidations.FullRefundMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Refund),
            cbPreviousReceiptReference = "NONEXISTENT",
            cbChargeItems = new List<ChargeItem> { new ChargeItem { Amount = -50m } },
            cbPayItems = new List<PayItem> { new PayItem { Amount = -50m } }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region VoidMustMatchOriginal

    [Fact]
    public async Task Void_MatchesOriginal_ShouldPass()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Quantity = 1, Amount = 100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Description = "Cash", Quantity = 1, Amount = 100m }
            }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.VoidMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Quantity = -1, Amount = -100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem { Description = "Cash", Quantity = -1, Amount = -100m }
            }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Void_DifferentAmount_ShouldFail()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Quantity = 1, Amount = 100m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem> { new PayItem { Description = "Cash", Amount = 100m } }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.VoidMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase)((long)ReceiptCase.PointOfSaleReceipt0x0001 | (long)ReceiptCaseFlags.Void),
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product", Quantity = -1, Amount = -90m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem> { new PayItem { Description = "Cash", Amount = -90m } }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "VoidItemsMismatch");
    }

    #endregion

    #region PartialRefundMustMatchOriginal

    [Fact]
    public async Task PartialRefund_MatchesOriginal_ShouldPass()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product A", Quantity = 1, Amount = 50m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate },
                new ChargeItem { Description = "Product B", Quantity = 1, Amount = 30m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 80m } }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.PartialRefundMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "REFUND-001",
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Product A", Quantity = -1, Amount = -50m, VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.Refund)
                }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = -50m } }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PartialRefund_ItemNotInOriginal_ShouldFail()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product A", Quantity = 1, Amount = 50m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 50m } }
        };
        var provider = CreateProvider(originalReceipt: original);
        var validator = new ReceiptValidations.PartialRefundMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "REFUND-001",
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Product C", Quantity = -1, Amount = -70m, VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.Refund)
                }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = -70m } }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PartialRefundItemsMismatch");
    }

    [Fact]
    public async Task PartialRefund_AlreadyConsumedByPriorRefund_ShouldFail()
    {
        var original = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem { Description = "Product A", Quantity = 1, Amount = 50m, VATRate = 23m, ftChargeItemCase = ChargeItemCase.NormalVatRate }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = 50m } }
        };

        var existingRefund = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "REFUND-001",
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Product A", Quantity = -1, Amount = -50m, VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.Refund)
                }
            }
        };

        var provider = CreateProvider(originalReceipt: original, existingPartialRefunds: new List<ReceiptRequest> { existingRefund });
        var validator = new ReceiptValidations.PartialRefundMustMatchOriginal(provider);

        var request = new ReceiptRequest
        {
            ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001,
            cbReceiptReference = "REFUND-002",
            cbPreviousReceiptReference = "ORIG-001",
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Description = "Product A", Quantity = -1, Amount = -50m, VATRate = 23m,
                    ftChargeItemCase = (ChargeItemCase)((long)ChargeItemCase.NormalVatRate | (long)ChargeItemCaseFlags.Refund)
                }
            },
            cbPayItems = new List<PayItem> { new PayItem { Amount = -50m } }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "PartialRefundItemsMismatch");
    }

    #endregion
}

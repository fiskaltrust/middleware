using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.SCU.GR.MyData;
using System;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.SCU.MyData;

public class AADEMappingsIncomeClassificationCategoryTypeTests
{
    private readonly ReceiptRequest _baseRequest = new ReceiptRequest
    {
        cbTerminalID = "1",
        Currency = Currency.EUR,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        ftPosSystemId = Guid.NewGuid()
    };

    private ChargeItem CreateChargeItem(ChargeItemCaseTypeOfService typeOfService)
    {
        return new ChargeItem
        {
            Position = 1,
            Amount = 100,
            VATRate = 24,
            VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithTypeOfService(typeOfService).WithVat(ChargeItemCase.NormalVatRate),
            Quantity = 1,
            Description = "Test Item"
        };
    }

    private ReceiptRequest CreateReceiptRequest(ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001)
    {
        return new ReceiptRequest
        {
            cbTerminalID = _baseRequest.cbTerminalID,
            Currency = _baseRequest.Currency,
            cbReceiptMoment = _baseRequest.cbReceiptMoment,
            cbReceiptReference = _baseRequest.cbReceiptReference,
            ftPosSystemId = _baseRequest.ftPosSystemId,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase)
        };
    }

    [Fact]
    public void GetIncomeClassificationCategoryType_WithInternalUsageMaterialConsumption_ReturnsCategory1_6()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.InternalUsageMaterialConsumption0x3003);
        var chargeItem = CreateChargeItem(ChargeItemCaseTypeOfService.Delivery); // TypeOfService doesn't matter for this case

        // Act
        var result = AADEMappings.GetIncomeClassificationCategoryType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationCategoryType.category1_6);
    }

    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.UnknownService, IncomeClassificationCategoryType.category1_1)]
    [InlineData(ChargeItemCaseTypeOfService.Delivery, IncomeClassificationCategoryType.category1_1)]
    [InlineData(ChargeItemCaseTypeOfService.OtherService, IncomeClassificationCategoryType.category1_3)]
    public void GetIncomeClassificationCategoryType_WithDifferentServiceTypes_ReturnsExpectedCategory(ChargeItemCaseTypeOfService serviceType, IncomeClassificationCategoryType expectedCategory)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(serviceType);

        // Act
        var result = AADEMappings.GetIncomeClassificationCategoryType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.Tip)]
    [InlineData(ChargeItemCaseTypeOfService.Voucher)]
    [InlineData(ChargeItemCaseTypeOfService.CatalogService)]
    [InlineData(ChargeItemCaseTypeOfService.NotOwnSales)]
    [InlineData(ChargeItemCaseTypeOfService.OwnConsumption)]
    [InlineData(ChargeItemCaseTypeOfService.Grant)]
    [InlineData(ChargeItemCaseTypeOfService.Receivable)]
    [InlineData(ChargeItemCaseTypeOfService.CashTransfer)]
    public void GetIncomeClassificationCategoryType_WithNonSupportedType_ReturnsException(ChargeItemCaseTypeOfService serviceType)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(serviceType);


        var action = () => AADEMappings.GetIncomeClassificationCategoryType(receiptRequest, chargeItem);
        action.Should().Throw<Exception>()
            .WithMessage($"The ChargeItem type {chargeItem.ftChargeItemCase.TypeOfService()} is not supported for IncomeClassificationCategoryType.");
    }
}

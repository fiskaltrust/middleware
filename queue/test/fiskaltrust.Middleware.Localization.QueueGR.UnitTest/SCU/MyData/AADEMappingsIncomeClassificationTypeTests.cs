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

public class AADEMappingsIncomeClassificationTypeTests
{
    private readonly ReceiptRequest _baseRequest = new ReceiptRequest
    {
        cbTerminalID = "1",
        Currency = Currency.EUR,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        ftPosSystemId = Guid.NewGuid()
    };

    private ChargeItem CreateChargeItem(decimal amount = 100, decimal vatAmount = 24, ChargeItemCaseTypeOfService typeOfService = ChargeItemCaseTypeOfService.Delivery)
    {
        return new ChargeItem
        {
            Position = 1,
            Amount = amount,
            VATRate = 24,
            VATAmount = vatAmount,
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithTypeOfService(typeOfService).WithVat(ChargeItemCase.NormalVatRate),
            Quantity = 1,
            Description = "Test Item"
        };
    }

    private ReceiptRequest CreateReceiptRequest(ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001, bool isRefund = false)
    {
        var request = new ReceiptRequest
        {
            cbTerminalID = _baseRequest.cbTerminalID,
            Currency = _baseRequest.Currency,
            cbReceiptMoment = _baseRequest.cbReceiptMoment,
            cbReceiptReference = _baseRequest.cbReceiptReference,
            ftPosSystemId = _baseRequest.ftPosSystemId,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase)
        };

        if (isRefund)
        {
            request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        }

        return request;
    }

    #region Tests for Order and PaymentTransfer cases (return category1_95)

    [Fact]
    public void GetIncomeClassificationType_WithOrder_ReturnsCategory1_95AndCorrectAmount()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.Order0x3004);
        var chargeItem = CreateChargeItem(amount: 124, vatAmount: 24);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100); // 124 - 24 = 100 (net amount)
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        result.classificationTypeSpecified.Should().BeFalse();
    }

    [Fact]
    public void GetIncomeClassificationType_WithPaymentTransfer_ReturnsCategory1_95AndCorrectAmount()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.PaymentTransfer0x0002);
        var chargeItem = CreateChargeItem(amount: 124, vatAmount: 24);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100); // 124 - 24 = 100 (net amount)
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        result.classificationTypeSpecified.Should().BeFalse();
    }

    [Fact]
    public void GetIncomeClassificationType_WithOrderAndRefund_ReturnsCategory1_95AndCorrectAmount()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.Order0x3004, isRefund: true);
        var chargeItem = CreateChargeItem(amount: -124, vatAmount: -24);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        // For refund: -chargeItem.Amount - -vatAmount = -(-124) - -(-24) = 124 - 24 = 100
        result.amount.Should().Be(100); 
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        result.classificationTypeSpecified.Should().BeFalse();
    }

    [Fact]
    public void GetIncomeClassificationType_WithPaymentTransferAndRefund_ReturnsCategory1_95AndCorrectAmount()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.PaymentTransfer0x0002, isRefund: true);
        var chargeItem = CreateChargeItem(amount: -124, vatAmount: -24);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        // For refund: -chargeItem.Amount - -vatAmount = -(-124) - -(-24) = 124 - 24 = 100
        result.amount.Should().Be(100);
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        result.classificationTypeSpecified.Should().BeFalse();
    }

    #endregion

    #region Tests for regular cases (return full classification)

    [Fact]
    public void GetIncomeClassificationType_WithRegularCase_ReturnsFullClassificationForDelivery()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(amount: 124, vatAmount: 24, typeOfService: ChargeItemCaseTypeOfService.Delivery);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100); // 124 - 24 = 100 (net amount)
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_1); // Based on GetIncomeClassificationCategoryType
        result.classificationType.Should().Be(IncomeClassificationValueType.E3_561_003); // Based on GetIncomeClassificationValueType for Receipt
        result.classificationTypeSpecified.Should().BeTrue();
    }

    [Fact]
    public void GetIncomeClassificationType_WithRegularCase_ReturnsFullClassificationForOtherService()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(amount: 124, vatAmount: 24, typeOfService: ChargeItemCaseTypeOfService.OtherService);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100); // 124 - 24 = 100 (net amount)
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3); // Based on GetIncomeClassificationCategoryType
        result.classificationType.Should().Be(IncomeClassificationValueType.E3_561_003); // Based on GetIncomeClassificationValueType for Receipt
        result.classificationTypeSpecified.Should().BeTrue();
    }

    [Fact]
    public void GetIncomeClassificationType_WithRegularCase_ReturnsFullClassificationForUnknownService()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(amount: 124, vatAmount: 24, typeOfService: ChargeItemCaseTypeOfService.UnknownService);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100); // 124 - 24 = 100 (net amount)
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95); // Based on GetIncomeClassificationCategoryType
        result.classificationTypeSpecified.Should().BeFalse();
    }

    [Fact]
    public void GetIncomeClassificationType_WithInternalUsageMaterialConsumption_ReturnsCorrectClassification()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.InternalUsageMaterialConsumption0x3003);
        var chargeItem = CreateChargeItem(amount: 124, vatAmount: 24, typeOfService: ChargeItemCaseTypeOfService.Delivery);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100); // 124 - 24 = 100 (net amount)
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_6); // Based on GetIncomeClassificationCategoryType
        result.classificationType.Should().Be(IncomeClassificationValueType.E3_595); // Based on GetIncomeClassificationValueType
        result.classificationTypeSpecified.Should().BeTrue();
    }

    #endregion

    #region Tests for refund scenarios

    [Fact]
    public void GetIncomeClassificationType_WithRefundRegularCase_ReturnsCorrectAmount()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(isRefund: true);
        var chargeItem = CreateChargeItem(amount: -124, vatAmount: -24);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        // For refund: -chargeItem.Amount - -vatAmount = -(-124) - -(-24) = 124 - 24 = 100
        result.amount.Should().Be(100);
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_1);
        result.classificationType.Should().Be(IncomeClassificationValueType.E3_561_003);
        result.classificationTypeSpecified.Should().BeTrue();
    }

    #endregion

    #region Tests for amount calculation edge cases

    [Fact]
    public void GetIncomeClassificationType_WithZeroVatAmount_CalculatesCorrectNetAmount()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(amount: 100, vatAmount: 0);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100); // 100 - 0 = 100
    }

    [Fact]
    public void GetIncomeClassificationType_WithDecimalAmounts_CalculatesCorrectNetAmount()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(amount: 123.45m, vatAmount: 23.45m);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(100.00m); // 123.45 - 23.45 = 100.00
    }

    #endregion

    #region Tests to verify delegation to other methods

    [Fact]
    public void GetIncomeClassificationType_CallsGetIncomeClassificationCategoryType()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(typeOfService: ChargeItemCaseTypeOfService.OtherService);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert - Verify that the result contains the expected category from GetIncomeClassificationCategoryType
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_3);
    }

    [Fact]
    public void GetIncomeClassificationType_CallsGetIncomeClassificationValueType()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(typeOfService: ChargeItemCaseTypeOfService.Delivery);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert - Verify that the result contains the expected value type from GetIncomeClassificationValueType
        result.classificationType.Should().Be(IncomeClassificationValueType.E3_561_003);
    }

    #endregion

    #region Tests for different receipt case combinations

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001)]
    [InlineData(ReceiptCase.DeliveryNote0x0005)]
    [InlineData(ReceiptCase.Pay0x3005)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003)]
    public void GetIncomeClassificationType_WithDifferentReceiptCases_ReturnsValidClassification(ReceiptCase receiptCase)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase);
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(76); // 100 - 24 = 76 (net amount)
        
        // For Order and PaymentTransfer cases, classificationTypeSpecified should be false
        if (receiptCase == ReceiptCase.Order0x3004 || receiptCase == ReceiptCase.PaymentTransfer0x0002)
        {
            result.classificationTypeSpecified.Should().BeFalse();
            result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        }
        else
        {
            result.classificationTypeSpecified.Should().BeTrue();
            result.classificationCategory.Should().NotBe(IncomeClassificationCategoryType.category1_95);
        }
    }

    #endregion

    #region Additional comprehensive test cases

    [Fact]
    public void GetIncomeClassificationType_WithDeliveryNote_ReturnsExpectedClassification()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.DeliveryNote0x0005);
        var chargeItem = CreateChargeItem(amount: 100, vatAmount: 20, typeOfService: ChargeItemCaseTypeOfService.Delivery);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(80); // 100 - 20 = 80
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_1);
        result.classificationType.Should().Be(IncomeClassificationValueType.E3_561_001); // Based on DeliveryNote case
        result.classificationTypeSpecified.Should().BeTrue();
    }

    [Fact]
    public void GetIncomeClassificationType_WithPay_ReturnsExpectedClassification()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.Pay0x3005);
        var chargeItem = CreateChargeItem(amount: 100, vatAmount: 20, typeOfService: ChargeItemCaseTypeOfService.UnknownService);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(80); // 100 - 20 = 80
        result.classificationCategory.Should().Be(IncomeClassificationCategoryType.category1_95);
        result.classificationTypeSpecified.Should().BeFalse();
    }

    [Theory]
    [InlineData(50.0, 10.0, 40.0)]
    [InlineData(1000.99, 240.24, 760.75)]
    [InlineData(0.01, 0.0, 0.01)]
    [InlineData(999999.99, 199999.99, 800000.00)]
    public void GetIncomeClassificationType_WithVariousAmounts_CalculatesCorrectNetAmount(decimal amount, decimal vatAmount, decimal expectedNet)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(amount: amount, vatAmount: vatAmount);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        result.amount.Should().Be(expectedNet);
    }

    [Fact]
    public void GetIncomeClassificationType_WithNegativeRegularAmounts_CalculatesCorrectNetAmount()
    {
        // Arrange - Regular negative amounts (not refund flag)
        var receiptRequest = CreateReceiptRequest();
        var chargeItem = CreateChargeItem(amount: -100, vatAmount: -20);

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        // For non-refund: chargeItem.Amount - vatAmount = -100 - (-20) = -100 + 20 = -80
        result.amount.Should().Be(-80);
        result.classificationTypeSpecified.Should().BeTrue();
    }

    #endregion

    #region Tests for VAT amount retrieval

    [Fact]
    public void GetIncomeClassificationType_UsesGetVATAmountExtensionMethod()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest();
        // Create a charge item where VATAmount property differs from calculated VAT
        var chargeItem = new ChargeItem
        {
            Position = 1,
            Amount = 124,
            VATRate = 24,
            VATAmount = 30, // This should be used instead of calculated VAT
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithTypeOfService(ChargeItemCaseTypeOfService.Delivery).WithVat(ChargeItemCase.NormalVatRate),
            Quantity = 1,
            Description = "Test Item"
        };

        // Act
        var result = AADEMappings.GetIncomeClassificationType(receiptRequest, chargeItem);

        // Assert
        result.Should().NotBeNull();
        // Should use the actual VATAmount (30) not calculated (24): 124 - 30 = 94
        result.amount.Should().Be(94);
    }

    #endregion
}
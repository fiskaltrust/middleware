using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;
using fiskaltrust.Middleware.SCU.GR.MyData;
using System;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest.SCU.MyData;

public class AADEMappingsIncomeClassificationValueTypeTests
{
    private ChargeItem CreateChargeItem(ChargeItemCaseTypeOfService typeOfService = ChargeItemCaseTypeOfService.Delivery, ChargeItemCaseNatureOfVatGR natureOfVat = ChargeItemCaseNatureOfVatGR.UsualVatApplies)
    {
        return new ChargeItem
        {
            Position = 1,
            Amount = 100,
            VATRate = 24,
            VATAmount = 24,
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                .WithTypeOfService(typeOfService)
                .WithVat(ChargeItemCase.NormalVatRate)
                .WithNatureOfVat(natureOfVat),
            Quantity = 1,
            Description = "Test Item"
        };
    }

    private ReceiptRequest CreateReceiptRequest(ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001, CustomerCountryCategory customerCountry = CustomerCountryCategory.Domestic)
    {
        var request = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            // Build the receipt case properly - start with base and apply case and type
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase)
        };

        // Set customer based on country category
        if (customerCountry != CustomerCountryCategory.Domestic)
        {
            var customerCountryCode = customerCountry switch
            {
                CustomerCountryCategory.EU => "DE", // Germany as EU example
                CustomerCountryCategory.ThirdCountry => "US", // United States as third country example
                _ => null
            };

            request.cbCustomer = new MiddlewareCustomer
            {
                CustomerCountry = customerCountryCode
            };
        }

        return request;
    }

    #region Tests for specific receipt case scenarios (early returns)

    [Fact]
    public void GetIncomeClassificationValueType_WithDeliveryNote_ReturnsE3_561_001()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.DeliveryNote0x0005);
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_001);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithPay_ReturnsE3_562()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.Pay0x3005);
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_562);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithInternalUsageMaterialConsumption_ReturnsE3_595()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(ReceiptCase.InternalUsageMaterialConsumption0x3003);
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_595);
    }

    #endregion

    #region Tests for NotOwnSales scenarios

    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, CustomerCountryCategory.EU, IncomeClassificationValueType.E3_881_003)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, CustomerCountryCategory.ThirdCountry, IncomeClassificationValueType.E3_881_004)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, CustomerCountryCategory.Domestic, IncomeClassificationValueType.E3_881_002)]
    public void GetIncomeClassificationValueType_WithNotOwnSalesReceipt_ReturnsCorrectValue(ReceiptCase receiptCase, CustomerCountryCategory customerCountry, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: receiptCase, customerCountry: customerCountry);
        var chargeItem = CreateChargeItem(ChargeItemCaseTypeOfService.NotOwnSales);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(CustomerCountryCategory.EU, IncomeClassificationValueType.E3_881_003)]
    [InlineData(CustomerCountryCategory.Domestic, IncomeClassificationValueType.E3_881_001)]
    public void GetIncomeClassificationValueType_WithNotOwnSalesInvoice_ReturnsCorrectValue(CustomerCountryCategory customerCountry, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002, customerCountry: customerCountry);
        var chargeItem = CreateChargeItem(ChargeItemCaseTypeOfService.NotOwnSales);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithNotOwnSalesInvoiceThirdCountry_ThrowsException()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002, customerCountry: CustomerCountryCategory.ThirdCountry);
        var chargeItem = CreateChargeItem(ChargeItemCaseTypeOfService.NotOwnSales);

        // Act & Assert
        var action = () => AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);
        action.Should().Throw<Exception>()
            .WithMessage("Agency business with non EU customer is not supported");
    }

    #endregion

    #region Tests for Invoice scenarios with different customer countries

    [Theory]
    [InlineData(CustomerCountryCategory.EU, IncomeClassificationValueType.E3_561_005)]
    [InlineData(CustomerCountryCategory.ThirdCountry, IncomeClassificationValueType.E3_561_006)]
    public void GetIncomeClassificationValueType_WithInvoiceForeignCustomer_ReturnsCorrectValue(CustomerCountryCategory customerCountry, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002, customerCountry: customerCountry);
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithInvoiceDomesticCustomerArticle39a_ReturnsE3_561_002()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002);
        var chargeItem = CreateChargeItem(natureOfVat: ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_002);
    }

    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.UnknownService, IncomeClassificationValueType.E3_561_001)]
    [InlineData(ChargeItemCaseTypeOfService.Delivery, IncomeClassificationValueType.E3_561_001)]
    [InlineData(ChargeItemCaseTypeOfService.CatalogService, IncomeClassificationValueType.E3_561_001)]
    [InlineData(ChargeItemCaseTypeOfService.OtherService, IncomeClassificationValueType.E3_561_001)]
    public void GetIncomeClassificationValueType_WithInvoiceDomesticCustomerDifferentServices_ReturnsE3_561_001(ChargeItemCaseTypeOfService serviceType, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002);
        var chargeItem = CreateChargeItem(serviceType);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithInvoiceDomesticCustomerUnsupportedService_ReturnsE3_561_007()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002);
        var chargeItem = CreateChargeItem(ChargeItemCaseTypeOfService.Tip); // Unsupported service type

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_007);
    }

    #endregion

    #region Tests for Receipt scenarios

    [Fact]
    public void GetIncomeClassificationValueType_WithReceiptArticle39a_ReturnsE3_561_004()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.PointOfSaleReceipt0x0001);
        var chargeItem = CreateChargeItem(natureOfVat: ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_004);
    }

    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.UnknownService, IncomeClassificationValueType.E3_561_003)]
    [InlineData(ChargeItemCaseTypeOfService.Delivery, IncomeClassificationValueType.E3_561_003)]
    [InlineData(ChargeItemCaseTypeOfService.CatalogService, IncomeClassificationValueType.E3_561_003)]
    [InlineData(ChargeItemCaseTypeOfService.OtherService, IncomeClassificationValueType.E3_561_003)]
    public void GetIncomeClassificationValueType_WithReceiptDifferentServices_ReturnsE3_561_003(ChargeItemCaseTypeOfService serviceType, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.PointOfSaleReceipt0x0001);
        var chargeItem = CreateChargeItem(serviceType);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithReceiptUnsupportedService_ReturnsE3_561_007()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.PointOfSaleReceipt0x0001);
        var chargeItem = CreateChargeItem(ChargeItemCaseTypeOfService.Tip); // Unsupported service type

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_007);
    }

    #endregion

    #region Tests for fallback scenarios (last switch statement in the method)

    [Theory]
    [InlineData(ChargeItemCaseTypeOfService.UnknownService, IncomeClassificationValueType.E3_561_003)]
    [InlineData(ChargeItemCaseTypeOfService.Delivery, IncomeClassificationValueType.E3_561_003)]
    [InlineData(ChargeItemCaseTypeOfService.OtherService, IncomeClassificationValueType.E3_561_003)]
    [InlineData(ChargeItemCaseTypeOfService.CatalogService, IncomeClassificationValueType.E3_561_007)]
    [InlineData(ChargeItemCaseTypeOfService.Tip, IncomeClassificationValueType.E3_561_007)]
    [InlineData(ChargeItemCaseTypeOfService.Voucher, IncomeClassificationValueType.E3_561_007)]
    public void GetIncomeClassificationValueType_WithFallbackScenarios_ReturnsCorrectValue(ChargeItemCaseTypeOfService serviceType, IncomeClassificationValueType expectedValue)
    {
        // Arrange - Using a log receipt case that doesn't match specific conditions and falls through to final switch
        var receiptRequest = CreateReceiptRequest(ReceiptCase.ProtocolUnspecified0x3000);
        var chargeItem = CreateChargeItem(serviceType);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    #endregion

    #region Edge case tests

    [Fact]
    public void GetIncomeClassificationValueType_WithNullCustomer_TreatsAsDomestic()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002);
        receiptRequest.cbCustomer = null; // Explicitly set to null
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_001);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithEmptyCustomerCountry_TreatsAsDomestic()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002);
        receiptRequest.cbCustomer = new MiddlewareCustomer { CustomerCountry = "" };
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_001);
    }

    [Fact]
    public void GetIncomeClassificationValueType_WithGreekCustomer_TreatsAsDomestic()
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002);
        receiptRequest.cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR" };
        var chargeItem = CreateChargeItem();

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(IncomeClassificationValueType.E3_561_001);
    }

    #endregion

    #region Comprehensive test scenarios covering all paths

    [Theory]
    [InlineData(ReceiptCase.DeliveryNote0x0005, ChargeItemCaseTypeOfService.Delivery, CustomerCountryCategory.Domestic, IncomeClassificationValueType.E3_561_001)]
    [InlineData(ReceiptCase.Pay0x3005, ChargeItemCaseTypeOfService.UnknownService, CustomerCountryCategory.Domestic, IncomeClassificationValueType.E3_562)]
    [InlineData(ReceiptCase.InternalUsageMaterialConsumption0x3003, ChargeItemCaseTypeOfService.Delivery, CustomerCountryCategory.Domestic, IncomeClassificationValueType.E3_595)]
    public void GetIncomeClassificationValueType_WithSpecialReceiptCases_ReturnsCorrectValue(ReceiptCase receiptCase, ChargeItemCaseTypeOfService serviceType, CustomerCountryCategory customerCountry, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase, customerCountry: customerCountry);
        var chargeItem = CreateChargeItem(serviceType);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(ReceiptCase.InvoiceB2B0x1002, CustomerCountryCategory.EU, ChargeItemCaseTypeOfService.Delivery, IncomeClassificationValueType.E3_561_005)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002, CustomerCountryCategory.ThirdCountry, ChargeItemCaseTypeOfService.Delivery, IncomeClassificationValueType.E3_561_006)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, CustomerCountryCategory.Domestic, ChargeItemCaseTypeOfService.Delivery, IncomeClassificationValueType.E3_561_003)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002, CustomerCountryCategory.Domestic, ChargeItemCaseTypeOfService.Delivery, IncomeClassificationValueType.E3_561_001)]
    public void GetIncomeClassificationValueType_WithReceiptTypeAndCustomerCountryCombinations_ReturnsCorrectValue(ReceiptCase receiptCase, CustomerCountryCategory customerCountry, ChargeItemCaseTypeOfService serviceType, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: receiptCase, customerCountry: customerCountry);
        var chargeItem = CreateChargeItem(serviceType);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    #endregion

    #region Tests for different VAT natures

    [Theory]
    [InlineData(ChargeItemCaseNatureOfVatGR.UsualVatApplies, IncomeClassificationValueType.E3_561_001)] // Should not affect result for Invoice
    [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableIntraCommunitySupplies, IncomeClassificationValueType.E3_561_001)] // Should not affect result unless Article39a
    [InlineData(ChargeItemCaseNatureOfVatGR.NotTaxableArticle39a, IncomeClassificationValueType.E3_561_002)] // Special case
    public void GetIncomeClassificationValueType_WithDifferentVatNatures_ReturnsCorrectValue(ChargeItemCaseNatureOfVatGR natureOfVat, IncomeClassificationValueType expectedValue)
    {
        // Arrange
        var receiptRequest = CreateReceiptRequest(receiptCase: ReceiptCase.InvoiceB2B0x1002);
        var chargeItem = CreateChargeItem(natureOfVat: natureOfVat);

        // Act
        var result = AADEMappings.GetIncomeClassificationValueType(receiptRequest, chargeItem);

        // Assert
        result.Should().Be(expectedValue);
    }

    #endregion
}